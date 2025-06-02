using System.Collections.Generic;
using DataStructures.Queue.ECS;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Entities;
using Unity.Burst;
using Unity.Jobs;
using System;

namespace Effects.ECS
{
    [UpdateInGroup(typeof(SimulationSystemGroup)), UpdateAfter(typeof(EnemyHashGridSystem))]
    public partial class CollisionSystem : SystemBase
    {
        public static readonly Dictionary<int, Action<Entity>> DamageDoneEvent = new Dictionary<int, Action<Entity>>();
            
        private ComponentLookup<LocalTransform> transformLookup;
        private NativeQueue<Entity> collisionQueue;
        private EntityQuery collisionQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            
            collisionQuery = SystemAPI.QueryBuilder().WithAspect<ColliderAspect>().Build();

            transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            collisionQueue = new NativeQueue<Entity>(Allocator.Persistent);
            
            RequireForUpdate<WaveStateComponent>();
            RequireForUpdate<SpatialHashMapSingleton>();
        }
        
        protected override void OnUpdate()
        {
            if (collisionQuery.IsEmpty)
            {
                return;
            }

            int enemyCount = SystemAPI.GetSingleton<WaveStateComponent>().EnemyCount;
            NativeParallelMultiHashMap<Entity, PendingDamageComponent> pendingDamageMap = new NativeParallelMultiHashMap<Entity, PendingDamageComponent>(enemyCount * 2 + 10, WorldUpdateAllocator);

            NativeParallelMultiHashMap<int2, Entity> spatialGrid = SystemAPI.GetSingletonRW<SpatialHashMapSingleton>().ValueRO.Value;
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
            transformLookup.Update(this);
             
            Dependency = new CollisionJob
            {
                PendingDamageMap = pendingDamageMap.AsParallelWriter(),
                CollisionQueue = collisionQueue.AsParallelWriter(),
                SpatialGrid = spatialGrid.AsReadOnly(),
                TransformLookup = transformLookup,
                ECB = ecb.AsParallelWriter(),
            }.ScheduleParallel(Dependency);
            Dependency.Complete(); 

            Dependency = new SumCollisionJob
            {
                ECB = ecb,
                PendingDamageMap = pendingDamageMap.AsReadOnly(),
            }.Schedule(Dependency);
            
            Dependency.Complete(); 
            ecb.Playback(EntityManager);
            ecb.Dispose();

            pendingDamageMap.Dispose();
            
            while (collisionQueue.TryDequeue(out Entity entity))
            {
                if (DamageDoneEvent.TryGetValue(EntityManager.GetComponentData<DamageComponent>(entity).Key, out var action))
                {
                    action.Invoke(entity);
                }
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            collisionQueue.Dispose();
        }

    }

    [BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
    public partial struct CollisionJob : IJobEntity
    {
        [ReadOnly]
        public NativeParallelMultiHashMap<int2, Entity>.ReadOnly SpatialGrid;

        [ReadOnly]
        public ComponentLookup<LocalTransform> TransformLookup;
        
        public NativeParallelMultiHashMap<Entity, PendingDamageComponent>.ParallelWriter PendingDamageMap;

        public EntityCommandBuffer.ParallelWriter ECB;

        public NativeQueue<Entity>.ParallelWriter CollisionQueue;

        [BurstCompile]
        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, ColliderAspect colliderAspect) // Cellsize = 1
        {
            if (colliderAspect.DamageComponent.ValueRO.LimitedHits <= 0)
            {
                return;
            }

            float3 pos = colliderAspect.PositionComponent.ValueRO.Position;
            float radius = colliderAspect.ColliderComponent.ValueRO.Radius;
            float radiusSq = radius * radius;

            int2 centerCell = new int2((int)pos.x, (int)pos.z);
            if (CollideWithinCell(entity, colliderAspect, centerCell, pos, radiusSq))
            {
                if (colliderAspect.DamageComponent.ValueRO.IsOneShot)
                {
                    ECB.AddComponent<DeathTag>(sortKey, entity);
                }
                return;
            }

            int searchRadius = (int)(radius + 0.5f);
            int minX = centerCell.x - searchRadius;
            int maxX = centerCell.x + searchRadius;
            int minZ = centerCell.y - searchRadius;
            int maxZ = centerCell.y + searchRadius;
            
            for (int z = minZ; z <= maxZ; z++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    int2 cell = new int2(x, z);
                    if (math.all(cell == centerCell)) continue;
                    
                    // Fast approximate distance check
                    float cellDistX = math.abs(x + 0.5f - pos.x);
                    float cellDistZ = math.abs(z + 0.5f - pos.z);
                    if (cellDistX > radius || cellDistZ > radius) continue;
                    
                    if (cellDistX * cellDistX + cellDistZ * cellDistZ > radiusSq) continue;
                    
                    if (CollideWithinCell(entity, colliderAspect, cell, pos, radiusSq)) return;
                }
            }

            if (colliderAspect.DamageComponent.ValueRO.IsOneShot)
            {
                ECB.AddComponent<DeathTag>(sortKey, entity);
            }
        }

        private bool CollideWithinCell(Entity entity, ColliderAspect colliderAspect, int2 cell, float3 pos, float radiusSq)
        {
            if (!SpatialGrid.TryGetFirstValue(cell, out Entity enemy, out var iterator)) return false;

            do
            {
                if (!TransformLookup.TryGetComponent(enemy, out LocalTransform enemyTransform)) continue;

                float distSq = math.distancesq(pos, enemyTransform.Position);

                if (distSq > radiusSq) continue;

                // COLLIDE
                PendingDamageComponent pendingDamage = GetDamage(colliderAspect, entity);
                PendingDamageMap.Add(enemy, pendingDamage);

                CollisionQueue.Enqueue(entity);

                if (colliderAspect.DamageComponent.ValueRO.HasLimitedHits)
                {
                    colliderAspect.DamageComponent.ValueRW.LimitedHits--;
                    if (colliderAspect.DamageComponent.ValueRO.LimitedHits == 0)
                    {
                        return true;
                    }
                }

            } while (SpatialGrid.TryGetNextValue(out enemy, ref iterator));

            return false;
        }

        private PendingDamageComponent GetDamage(ColliderAspect colliderAspect, Entity sourceEntity)
        {
            bool isCrit = colliderAspect.RandomComponent.ValueRW.Random.NextFloat() < colliderAspect.CritComponent.ValueRO.CritChance;
            float critMultiplier = isCrit ? colliderAspect.CritComponent.ValueRO.CritDamage : 1;
            return new PendingDamageComponent
            {
                HealthDamage = colliderAspect.DamageComponent.ValueRO.HealthDamage * critMultiplier,
                ArmorDamage = colliderAspect.DamageComponent.ValueRO.ArmorDamage * critMultiplier,
                ShieldDamage = colliderAspect.DamageComponent.ValueRO.ShieldDamage * critMultiplier,
                IsCrit = isCrit,
                
                SourceEntity = sourceEntity,
            };
        }
    }

    [BurstCompile]
    public struct SumCollisionJob : IJob
    {
        [ReadOnly]
        public NativeParallelMultiHashMap<Entity, PendingDamageComponent>.ReadOnly PendingDamageMap;
        
        public EntityCommandBuffer ECB;
        
        public void Execute()
        {
            // Get all unique keys (entities that were hit)
            NativeArray<Entity> keys = PendingDamageMap.GetKeyArray(Allocator.Temp);
            
            // Proper iteration pattern for NativeMultiHashMap
            foreach (Entity entity in keys)
            {
                if (!PendingDamageMap.TryGetFirstValue(entity, out PendingDamageComponent damage, out var iterator)) continue;

                PendingDamageComponent pendingDamage = new PendingDamageComponent
                {
                    SourceEntity = damage.SourceEntity
                };
                
                do 
                {
                    pendingDamage.HealthDamage += damage.HealthDamage;
                    pendingDamage.ArmorDamage += damage.ArmorDamage;
                    pendingDamage.ShieldDamage += damage.ShieldDamage;
                    pendingDamage.IsCrit |= damage.IsCrit;
                } 
                while (PendingDamageMap.TryGetNextValue(out damage, ref iterator));
                    
                ECB.AddComponent(entity, pendingDamage);
            }
            
            keys.Dispose();
        }
    }
}
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Entities;
using Unity.Burst;
using Unity.Jobs;
using Enemy.ECS;

namespace Effects.ECS
{
    [BurstCompile, UpdateInGroup(typeof(SimulationSystemGroup)), UpdateAfter(typeof(EnemyHashGridSystem))]
    public partial struct CollisionSystem : ISystem
    {
        private ComponentLookup<LocalTransform> transformLookup;
        private EntityQuery collisionQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            collisionQuery = SystemAPI.QueryBuilder().WithAll
                <ColliderComponent, DamageComponent, RandomComponent, CritComponent, LocalTransform>()
                .Build();
            
            transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            state.RequireForUpdate<WaveStateComponent>();
            state.RequireForUpdate<SpatialHashMapSingleton>();        
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (collisionQuery.IsEmpty)
            {
                return;
            }

            int enemyCount = SystemAPI.GetSingleton<WaveStateComponent>().EnemyCount;
            NativeParallelMultiHashMap<Entity, PendingDamageComponent> pendingDamageMap = new NativeParallelMultiHashMap<Entity, PendingDamageComponent>(enemyCount * 2 + 10, state.WorldUpdateAllocator);

            NativeParallelMultiHashMap<int2, Entity> spatialGrid = SystemAPI.GetSingletonRW<SpatialHashMapSingleton>().ValueRO.Value;
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
            transformLookup.Update(ref state);
             
            state.Dependency = new CollisionJob
            {
                PendingDamageMap = pendingDamageMap.AsParallelWriter(),
                SpatialGrid = spatialGrid.AsReadOnly(),
                TransformLookup = transformLookup,
                ECB = ecb.AsParallelWriter(),
            }.ScheduleParallel(state.Dependency);
            state.Dependency.Complete(); 

            state.Dependency = new SumCollisionJob
            {
                ECB = ecb,
                PendingDamageMap = pendingDamageMap.AsReadOnly(),
            }.Schedule(state.Dependency);
            state.Dependency.Complete(); 
            
            ecb.Playback(state.EntityManager);
            ecb.Dispose();

            pendingDamageMap.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            
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

        // Cellsize = 1
        [BurstCompile]
        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in ColliderComponent colliderComponent, ref DamageComponent damageComponent, ref RandomComponent randomComponent, in CritComponent critComponent, in LocalTransform transform) 
        {
            if (damageComponent.LimitedHits <= 0)
            {
                return;
            }

            float3 pos = transform.Position;
            float radius = colliderComponent.Radius;
            float radiusSq = radius * radius;

            int2 centerCell = new int2((int)pos.x, (int)pos.z);
            if (CollideWithinCell(entity, ref randomComponent, critComponent, ref damageComponent, centerCell, pos.xz, radiusSq))
            {
                if (damageComponent.IsOneShot)
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
                    
                    if (CollideWithinCell(entity, ref randomComponent, critComponent, ref damageComponent, cell, pos.xz, radiusSq)) return;
                }
            }

            if (damageComponent.IsOneShot)
            {
                ECB.AddComponent<DeathTag>(sortKey, entity);
            }
        }

        [BurstCompile]
        private bool CollideWithinCell(Entity entity, ref RandomComponent randomComponent, CritComponent critComponent, ref DamageComponent damageComponent, int2 cell, float2 pos, float radiusSq)
        {
            if (!SpatialGrid.TryGetFirstValue(cell, out Entity enemy, out var iterator)) return false;

            do
            {
                if (!TransformLookup.TryGetComponent(enemy, out LocalTransform enemyTransform)) continue;

                float distSq = math.distancesq(pos, enemyTransform.Position.xz);

                if (distSq > radiusSq) continue;

                // COLLIDE
                PendingDamageComponent pendingDamage = GetDamage(ref randomComponent, critComponent, ref damageComponent, entity);
                PendingDamageMap.Add(enemy, pendingDamage);

                if (damageComponent.HasLimitedHits)
                {
                    damageComponent.LimitedHits--;
                    if (damageComponent.LimitedHits == 0)
                    {
                        return true;
                    }
                }

            } while (SpatialGrid.TryGetNextValue(out enemy, ref iterator));

            return false;
        }

        [BurstCompile]
        private PendingDamageComponent GetDamage(ref RandomComponent randomComponent, CritComponent critComponent, ref DamageComponent damageComponent, Entity sourceEntity)
        {
            bool isCrit = randomComponent.Random.NextFloat() < critComponent.CritChance;
            float critMultiplier = isCrit ? critComponent.CritDamage : 1;
            return new PendingDamageComponent
            {
                HealthDamage = damageComponent.HealthDamage * critMultiplier,
                ArmorDamage = damageComponent.ArmorDamage * critMultiplier,
                ShieldDamage = damageComponent.ShieldDamage * critMultiplier,
                IsCrit = isCrit,
                Key = damageComponent.Key,
                TriggerDamageDone = damageComponent.TriggerDamageDone,
                
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
                    
                    pendingDamage.Key = damage.Key; // A bit inaccurate
                    pendingDamage.TriggerDamageDone = damage.TriggerDamageDone;
                } 
                while (PendingDamageMap.TryGetNextValue(out damage, ref iterator));
                    
                ECB.AddComponent(entity, pendingDamage);
            }
            
            keys.Dispose();
        }
    }
}
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Entities;
using Unity.Burst;
using Unity.Jobs;
using Enemy.ECS;
using Pathfinding;

namespace Effects.ECS
{
    [BurstCompile, UpdateAfter(typeof(EnemyHashGridSystem))]
    public partial struct CollisionSystem : ISystem
    {
        private ComponentLookup<LocalTransform> transformLookup;
        private BufferLookup<ManagedEntityBuffer> bufferLookup;
        private EntityQuery collisionQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            collisionQuery = SystemAPI.QueryBuilder().WithAll
                <ColliderComponent, DamageComponent, RandomComponent, CritComponent, LocalTransform>()
                .Build();
            
            transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            bufferLookup = SystemAPI.GetBufferLookup<ManagedEntityBuffer>();
            
            state.RequireForUpdate<WaveStateComponent>();
            state.RequireForUpdate<SpatialHashMapSingleton>();   
            state.RequireForUpdate<FlowFieldComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (collisionQuery.IsEmpty)
            {
                return;
            }

            int enemyCount = SystemAPI.GetSingleton<WaveStateComponent>().ClusterCount;
            NativeParallelMultiHashMap<Entity, PendingDamageComponent> pendingDamageMap = new NativeParallelMultiHashMap<Entity, PendingDamageComponent>(enemyCount * 2 + 10, state.WorldUpdateAllocator);

            NativeParallelHashMap<int, Entity> spatialGrid = SystemAPI.GetSingletonRW<SpatialHashMapSingleton>().ValueRO.Value;
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
            transformLookup.Update(ref state);
            bufferLookup.Update(ref state);
            state.Dependency = new CollisionJob
            {
                PendingDamageMap = pendingDamageMap.AsParallelWriter(),
                SpatialGrid = spatialGrid.AsReadOnly(),
                TransformLookup = transformLookup,
                ECB = ecb.AsParallelWriter(),
                BufferLookup = bufferLookup,
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
        public NativeParallelHashMap<int, Entity>.ReadOnly SpatialGrid;

        [ReadOnly]
        public ComponentLookup<LocalTransform> TransformLookup;
        
        [ReadOnly]
        public BufferLookup<ManagedEntityBuffer> BufferLookup;
        
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
            if (pos.y > 1.0f)
            {
                return;
            }          
            
            float radius = colliderComponent.Radius;
            float radiusSq = radius * radius;

            int centerCell = PathUtility.GetPathGridIndex(pos.xz);
            if (CollideWithinCell(entity, ref randomComponent, critComponent, ref damageComponent, centerCell, pos.xz, radiusSq))
            {
                if (damageComponent.IsOneShot)
                {
                    ECB.AddComponent<DeathTag>(sortKey, entity);
                }
                return;
            }

            int searchRadius = (int)(radius / PathUtility.CELL_SCALE + 0.5f); // 0.5 for rounding
            for (int z = -searchRadius; z <= searchRadius; z++)
            {
                for (int x = -searchRadius; x <= searchRadius; x++)
                {
                    if (z == 0 && x == 0) continue;
                    
                    // Fast approximate distance check
                    float cellDistX = math.abs(x + 0.5f - pos.x);
                    float cellDistZ = math.abs(z + 0.5f - pos.z);
                    if (cellDistX > radius || cellDistZ > radius) continue;
                    
                    if (cellDistX * cellDistX + cellDistZ * cellDistZ > radiusSq) continue;
                    
                    int cell = centerCell + x + z * PathUtility.CELL_SCALE;
                    if (CollideWithinCell(entity, ref randomComponent, critComponent, ref damageComponent, cell, pos.xz, radiusSq)) return;
                }
            }

            if (damageComponent.IsOneShot)
            {
                ECB.AddComponent<DeathTag>(sortKey, entity);
            }
        }

        [BurstCompile]
        private bool CollideWithinCell(Entity entity, ref RandomComponent randomComponent, CritComponent critComponent, ref DamageComponent damageComponent, int cell, float2 pos, float radiusSq)
        {
            if (!SpatialGrid.TryGetValue(cell, out Entity cluster) || !BufferLookup.TryGetBuffer(cluster, out DynamicBuffer<ManagedEntityBuffer> buffer)) return false;

            for (int i = 0; i < buffer.Length; i++)
            {
                Entity enemy = buffer[i].Entity;
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
            }

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
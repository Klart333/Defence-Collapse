using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Effects.ECS.ECB;
using Unity.Entities;
using Pathfinding;
using Unity.Burst;
using Enemy.ECS;

namespace Effects.ECS
{
    [BurstCompile, UpdateAfter(typeof(EnemyHashGridSystem)), 
     UpdateAfter(typeof(BeforeCollisionECBSystem)), UpdateBefore(typeof(BeforeDamageEffectsECBSystem))]
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
            
            state.RequireForUpdate<BeforeDamageEffectsECBSystem.Singleton>();
            state.RequireForUpdate<SpatialHashMapSingleton>();   
            state.RequireForUpdate(collisionQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            NativeParallelHashMap<int2, Entity> spatialGrid = SystemAPI.GetSingletonRW<SpatialHashMapSingleton>().ValueRO.Value;
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
            
            transformLookup.Update(ref state);
            bufferLookup.Update(ref state);
            
            state.Dependency = new CollisionJob
            {
                SpatialGrid = spatialGrid.AsReadOnly(),
                TransformLookup = transformLookup,
                ECB = ecb.AsParallelWriter(),
                BufferLookup = bufferLookup,
            }.ScheduleParallel(state.Dependency);
            
            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
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
        public NativeParallelHashMap<int2, Entity>.ReadOnly SpatialGrid;

        [ReadOnly]
        public ComponentLookup<LocalTransform> TransformLookup;
        
        [ReadOnly]
        public BufferLookup<ManagedEntityBuffer> BufferLookup;
        
        public EntityCommandBuffer.ParallelWriter ECB;

        [BurstCompile]
        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in ColliderComponent colliderComponent, 
            ref DamageComponent damageComponent, ref RandomComponent randomComponent, in CritComponent critComponent, 
            in LocalTransform transform) 
        {
            if (damageComponent.LimitedHits <= 0) return;
            if (transform.Position.y > 1.0f) return;
            
            float2 pos = transform.Position.xz;
            float radius = colliderComponent.Radius;
            float radiusSq = radius * radius;

            int2 centerCell = PathUtility.GetCombinedIndex(pos);
            if (CollideWithinCell(sortKey, entity, centerCell, ref randomComponent, critComponent, ref damageComponent, pos, radiusSq))
            {
                if (damageComponent.IsOneShot)
                {
                    ECB.AddComponent<DeathTag>(sortKey, entity);
                }
                return;
            }

            int searchRadius = (int)(radius / PathUtility.CELL_SCALE + 1f); // ceil that shit
            for (int x = -searchRadius; x <= searchRadius; x++)
            for (int z = -searchRadius; z <= searchRadius; z++)
            {
                if (z == 0 && x == 0) continue;
                
                int2 cell = new int2(centerCell.x + x, centerCell.y + z);
                if (CollideWithinCell(sortKey, entity, cell, ref randomComponent, critComponent, ref damageComponent, pos, radiusSq)) return;
            }

            if (damageComponent.IsOneShot)
            {
                ECB.AddComponent<DeathTag>(sortKey, entity);
            }
        }

        [BurstCompile]
        private bool CollideWithinCell(int sortKey, Entity entity, int2 cell, ref RandomComponent randomComponent, CritComponent critComponent, ref DamageComponent damageComponent, float2 pos, float radiusSq)
        {
            if (!SpatialGrid.TryGetValue(cell, out Entity cluster) || !BufferLookup.TryGetBuffer(cluster, out DynamicBuffer<ManagedEntityBuffer> buffer)) return false;

            for (int i = 0; i < buffer.Length; i++)
            {
                Entity enemy = buffer[i].Entity;
                if (!TransformLookup.TryGetComponent(enemy, out LocalTransform enemyTransform)) continue;

                float distSq = math.distancesq(pos, enemyTransform.Position.xz);

                if (distSq > radiusSq) continue;

                // COLLIDE
                AddDamageBuffer(sortKey, enemy, ref randomComponent, critComponent, ref damageComponent, entity);

                if (!damageComponent.HasLimitedHits) continue;
                
                damageComponent.LimitedHits--;
                if (damageComponent.LimitedHits == 0)
                {
                    return true;
                }
            }

            return false;
        }

        [BurstCompile]
        private void AddDamageBuffer(int sortKey, Entity entity, ref RandomComponent randomComponent, CritComponent critComponent, ref DamageComponent damageComponent, Entity sourceEntity)
        {
            bool isCrit = randomComponent.Random.NextFloat() < critComponent.CritChance;
            float critMultiplier = isCrit ? critComponent.CritDamage : 1;
            ECB.AppendToBuffer(sortKey, entity, new DamageBuffer
            {
                Damage = damageComponent.Damage * critMultiplier,
                ArmorPenetration = damageComponent.ArmorPenetration,
                
                TriggerDamageDone = damageComponent.TriggerDamageDone,
                Key = damageComponent.Key,
                IsCrit = isCrit,
                
                SourceEntity = sourceEntity,
            });
            
            ECB.AddComponent<PendingDamageTag>(sortKey, entity);
        }
    }
}
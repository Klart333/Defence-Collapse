using System;
using System.Collections.Generic;
using DataStructures.Queue.ECS;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Effects.ECS
{
    [UpdateInGroup(typeof(SimulationSystemGroup)), UpdateAfter(typeof(EnemyHashGridSystem))]
    public partial class CollisionSystem : SystemBase
    {
        public static readonly Dictionary<int, Action<Entity>> DamageDoneEvent = new Dictionary<int, Action<Entity>>();
            
        private EntityQuery collisionQuery;
        private NativeQueue<Entity> collisionQueue;

        protected override void OnCreate()
        {
            base.OnCreate();
            
            collisionQuery = SystemAPI.QueryBuilder()
                .WithAspect<ColliderAspect>()
                .Build();
            
            collisionQueue = new NativeQueue<Entity>(Allocator.Persistent);
        }
        
        protected override void OnUpdate()
        {
            if (collisionQuery.IsEmpty)
            {
                return;
            }
                         
            NativeParallelMultiHashMap<int2, Entity> spatialGrid = SystemAPI.GetSingletonRW<SpatialHashMapSingleton>().ValueRO.Value;
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
             
            new CollisionJob
            {
                SpatialGrid = spatialGrid.AsReadOnly(),
                TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
                HealthLookup = SystemAPI.GetComponentLookup<HealthComponent>(true),
                CollisionQueue = collisionQueue.AsParallelWriter(),
                //CellSize = 1,
                ECB = ecb.AsParallelWriter(),
            }.ScheduleParallel();
                         
            Dependency.Complete(); 
            ecb.Playback(EntityManager);
            ecb.Dispose();
             
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

    [BurstCompile]
    public partial struct CollisionJob : IJobEntity
    {
        [ReadOnly]
        public NativeParallelMultiHashMap<int2, Entity>.ReadOnly SpatialGrid;

        [ReadOnly]
        public ComponentLookup<LocalTransform> TransformLookup;

        [ReadOnly]
        public ComponentLookup<HealthComponent> HealthLookup;

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
            if (CollideWithinCell(sortKey, entity, colliderAspect, centerCell, pos, radiusSq))
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
                    
                    if (CollideWithinCell(sortKey, entity, colliderAspect, cell, pos, radiusSq)) return;
                }
            }

            if (colliderAspect.DamageComponent.ValueRO.IsOneShot)
            {
                ECB.AddComponent<DeathTag>(sortKey, entity);
            }
        }

        private bool CollideWithinCell(int sortKey, Entity entity, ColliderAspect colliderAspect, int2 cell, float3 pos, float radiusSq)
        {
            if (!SpatialGrid.TryGetFirstValue(cell, out Entity enemy, out var iterator)) return false;

            do
            {
                if (!TransformLookup.TryGetComponent(enemy, out LocalTransform enemyTransform)) continue;

                float distSq = math.distancesq(pos, enemyTransform.Position);

                if (distSq > radiusSq
                    || !HealthLookup.TryGetComponent(enemy, out HealthComponent health)) continue;

                // COLLIDE
                health.PendingDamage += colliderAspect.DamageComponent.ValueRO.Damage;
                ECB.SetComponent(sortKey, enemy, health);
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
    }
}
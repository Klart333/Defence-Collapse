using System;
using System.Collections.Generic;
using DataStructures.Queue.ECS;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Effects.ECS
{
    public partial struct CollisionSystem : ISystem
    {
        public static Dictionary<int, Action<DamageComponent>> DamageDoneEvent;
            
        private EntityQuery collisionQuery;
        private NativeQueue<DamageComponent> collisionQueue;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            collisionQuery = SystemAPI.QueryBuilder()
                .WithAspect<ColliderAspect>()
                .Build();
            
            collisionQueue = new NativeQueue<DamageComponent>(Allocator.Persistent);
            DamageDoneEvent = new Dictionary<int, Action<DamageComponent>>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (collisionQuery.IsEmpty)
            {
                return;
            }
            
            NativeParallelMultiHashMap<int2, Entity> spatialGrid = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<EnemyHashGridSystem>().SpatialGrid;

            new CollisionJob
            {
                SpatialGrid = spatialGrid.AsReadOnly(),
                ColliderLookup = SystemAPI.GetComponentLookup<ColliderComponent>(),
                TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(),
                HealthLookup = SystemAPI.GetComponentLookup<HealthComponent>(),
                CollisionQueue = collisionQueue.AsParallelWriter(),
            }.ScheduleParallel();
            
            state.Dependency.Complete();

            while (collisionQueue.TryDequeue(out DamageComponent damage))
            {
                if (DamageDoneEvent.TryGetValue(damage.Key, out var action))
                {
                    action.Invoke(damage);
                }
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            collisionQueue.Dispose();
        }
    }
    
    [BurstCompile]
    public partial struct CollisionJob : IJobEntity
    {
        [ReadOnly]
        public NativeParallelMultiHashMap<int2, Entity>.ReadOnly SpatialGrid;
        
        [ReadOnly]
        public ComponentLookup<ColliderComponent> ColliderLookup;

        [ReadOnly]
        public ComponentLookup<LocalTransform> TransformLookup;

        [ReadOnly]
        public ComponentLookup<HealthComponent> HealthLookup;
        
        public NativeQueue<DamageComponent>.ParallelWriter CollisionQueue;
        
        [ReadOnly]
        public float CellSize;
        
        [BurstCompile]
        public void Execute(ColliderAspect colliderAspect)
        {
            int2 cell = HashGridUtility.GetCell(colliderAspect.PositionComponent.ValueRO.Position, CellSize);
            float3 pos = colliderAspect.PositionComponent.ValueRO.Position;
            float colliderRadius = colliderAspect.ColliderComponent.ValueRO.Radius;
            
            if (!SpatialGrid.TryGetFirstValue(cell, out Entity enemy, out var iterator)) return;
                
            do
            {
                if (!TransformLookup.TryGetComponent(enemy, out LocalTransform enemyTransform) || !ColliderLookup.TryGetComponent(enemy, out ColliderComponent colliderComponent)) continue;
                
                float distSq = math.distancesq(pos, enemyTransform.Position);
                float radius = colliderComponent.Radius + colliderRadius;
                
                if (distSq < radius * radius && HealthLookup.TryGetComponent(enemy, out HealthComponent health))
                {
                    // COLLIDE
                    health.Health -= colliderAspect.DamageComponent.ValueRO.Damage; // Change later if resistances
                    colliderAspect.DamageComponent.ValueRW.LimitedHits--;
                    CollisionQueue.Enqueue(colliderAspect.DamageComponent.ValueRO);
                }

            } while (SpatialGrid.TryGetNextValue(out enemy, ref iterator));
        }
    }
}
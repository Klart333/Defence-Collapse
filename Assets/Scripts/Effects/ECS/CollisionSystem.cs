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
    [UpdateAfter(typeof(EnemyHashGridSystem))]
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
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
             
            new CollisionJob
            {
                SpatialGrid = spatialGrid.AsReadOnly(),
                TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
                HealthLookup = SystemAPI.GetComponentLookup<HealthComponent>(true),
                CollisionQueue = collisionQueue.AsParallelWriter(),
                CellSize = 1,
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
        
        [ReadOnly]
        public float CellSize;
        
        [BurstCompile]
        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, ColliderAspect colliderAspect)
        {
            if (colliderAspect.DamageComponent.ValueRO.LimitedHits <= 0)
            {
                return;
            }
            
            int2 cell = HashGridUtility.GetCell(colliderAspect.PositionComponent.ValueRO.Position, CellSize);
            float3 pos = colliderAspect.PositionComponent.ValueRO.Position;
            float colliderRadius = colliderAspect.ColliderComponent.ValueRO.Radius;
            
            if (!SpatialGrid.TryGetFirstValue(cell, out Entity enemy, out var iterator)) return;
                
            do
            {
                if (!TransformLookup.TryGetComponent(enemy, out LocalTransform enemyTransform)) continue;
                
                float distSq = math.distancesq(pos, enemyTransform.Position);

                if (distSq > colliderRadius * colliderRadius 
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
                        break;
                    }
                }

            } while (SpatialGrid.TryGetNextValue(out enemy, ref iterator));
        }
    }
}
using System;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.Entities;
using Unity.Burst;
using DG.Tweening;
using Effects.ECS;
using Enemy.ECS;

namespace Gameplay.Chunk.ECS
{
    [BurstCompile, UpdateBefore(typeof(ClusterPositioningSystem)), 
     UpdateAfter(typeof(FlowMovementSystem)), UpdateAfter(typeof(SpawnerSystem))]
    public partial struct AvoidClusterSystem : ISystem
    {
        private EntityQuery movedClustersQuery;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<UpdatePositioningComponent>();

            movedClustersQuery = SystemAPI.QueryBuilder().WithAll<EnemyClusterComponent, UpdatePositioningComponent>().Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            NativeArray<UpdatePositioningComponent> positions = movedClustersQuery.ToComponentDataArray<UpdatePositioningComponent>(Allocator.TempJob);

            state.Dependency = new AvoidClusterJob
            {
                ECB = ecb.AsParallelWriter(),
                PositioningComponents = positions,
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
    
    // Get the clusters that moved -> get the PathIndex -> compare with the treePathIndex -> Add smooth movement component
    // -> Don't move the trees already moving -> Spawn particles based on same as highlightclusterhandler
    [BurstCompile, WithNone(typeof(SmoothMovementComponent))]
    public partial struct AvoidClusterJob : IJobEntity
    {
        [ReadOnly, DeallocateOnJobCompletion]
        public NativeArray<UpdatePositioningComponent> PositioningComponents;
        
        public EntityCommandBuffer.ParallelWriter ECB;

        private const float Height = 2.0f;
        private const float GroundHeight = 0;
        
        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in GroundObjectComponent groundObjectComponent, in LocalTransform transform)
        {
            for (int i = 0; i < PositioningComponents.Length; ++i)
            {
                UpdatePositioningComponent posComp = PositioningComponents[i]; 
                if (posComp.PreviousTile.Equals(posComp.CurrentTile))
                {
                    continue;
                }

                if (posComp.CurrentTile.Equals(groundObjectComponent.PathIndex))
                {
                    HideGroundObject(sortKey, entity, transform);
                    break;
                }

                if (posComp.PreviousTile.Equals(groundObjectComponent.PathIndex))
                {
                    ShowGroundObject(sortKey, entity, transform);
                    break;
                }
            }
        }

        private void HideGroundObject(int sortKey, Entity entity, LocalTransform transform)
        {
            ECB.RemoveComponent<SmoothMovementComponent>(sortKey, entity);
            ECB.AddComponent(sortKey, entity, new SmoothMovementComponent
            {
                StartPosition = transform.Position,
                EndPosition = transform.Position.XyZ(-Height),
                Ease = Ease.InOutSine
            });
        }

        private void ShowGroundObject(int sortKey, Entity entity, LocalTransform transform)
        {
            ECB.RemoveComponent<SmoothMovementComponent>(sortKey, entity);
            ECB.AddComponent(sortKey, entity, new SmoothMovementComponent
            {
                StartPosition = transform.Position,
                EndPosition = transform.Position.XyZ(GroundHeight),
                Ease = Ease.InOutSine
            });
        }
    }
}
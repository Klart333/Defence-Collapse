using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Pathfinding.ECS;
using Unity.Entities;
using Unity.Burst;
using Pathfinding;

namespace Enemy.ECS
{
    [UpdateAfter(typeof(FlowMovementSystem))]
    public partial struct CheckAttackingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PathBlobber>();
            state.RequireForUpdate<EnemyClusterComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            Entity pathBlobberEntity = SystemAPI.GetSingletonEntity<PathBlobber>();
            PathBlobber pathBlobber = SystemAPI.GetComponent<PathBlobber>(pathBlobberEntity);
            
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

            new CheckAttackingJob
            {
                ECB = ecb.AsParallelWriter(),
                PathChunks = pathBlobber.PathBlob,
                ChunkIndexToListIndex = pathBlobber.ChunkIndexToListIndex,
            }.ScheduleParallel();
            
            state.Dependency.Complete(); 
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }

    [BurstCompile, WithAll(typeof(EnemyClusterComponent), typeof(UpdatePositioningTag)), WithNone(typeof(AttackingComponent))]
    public partial struct CheckAttackingJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;

        public BlobAssetReference<PathChunkArray> PathChunks;
        
        [ReadOnly, NativeDisableContainerSafetyRestriction]
        public NativeHashMap<int2, int>.ReadOnly ChunkIndexToListIndex;
        
        [BurstCompile]
        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in EnemyClusterComponent clusterComponent)
        {
            ref PathChunk valuePathChunk = ref PathChunks.Value.PathChunks[ChunkIndexToListIndex[clusterComponent.TargetPathIndex.ChunkIndex]];
            if (valuePathChunk.Directions[clusterComponent.TargetPathIndex.GridIndex] == byte.MaxValue)
            {
                ECB.AddComponent(sortKey, entity, new AttackingComponent { Target = clusterComponent.TargetPathIndex });
            }
        }
    }

    public struct AttackingComponent : IComponentData
    {
        public PathIndex Target;
    }
}
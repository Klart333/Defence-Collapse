using Pathfinding;
using Pathfinding.ECS;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace DataStructures.Queue.ECS
{
    public partial struct CheckAttackingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PathBlobber>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            Entity pathBlobberEntity = SystemAPI.GetSingletonEntity<PathBlobber>();
            PathBlobber pathBlobber = SystemAPI.GetComponent<PathBlobber>(pathBlobberEntity);
            
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

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

    [BurstCompile, WithNone(typeof(AttackingComponent))]
    public partial struct CheckAttackingJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;

        public BlobAssetReference<PathChunkArray> PathChunks;
        
        [ReadOnly, NativeDisableContainerSafetyRestriction]
        public NativeHashMap<int2, int>.ReadOnly ChunkIndexToListIndex;
        
        [BurstCompile]
        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in FlowFieldComponent flowField, in LocalTransform transform)
        {
            PathIndex index = PathManager.GetIndex(transform.Position.x, transform.Position.z);
            ref PathChunk valuePathChunk = ref PathChunks.Value.PathChunks[ChunkIndexToListIndex[index.ChunkIndex]];
            if (valuePathChunk.Directions[index.GridIndex] == byte.MaxValue)
            {
                ECB.AddComponent(sortKey, entity, new AttackingComponent { Target = index });
            }
        }
    }

    public struct AttackingComponent : IComponentData
    {
        public PathIndex Target;
    }
}
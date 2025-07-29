using Unity.Collections.LowLevel.Unsafe; 
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Pathfinding.ECS;
using Unity.Entities;
using Pathfinding;
using Unity.Jobs;
using Unity.Burst;
using Enemy.ECS;

namespace Effects.LittleDudes
{
    public partial struct LittleDudePathTargetSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<LittleDudePathBlobber>();
            
            EntityQuery littleDudeQuery = SystemAPI.QueryBuilder().WithAll<LittleDudeComponent>().Build();
            state.RequireForUpdate(littleDudeQuery);        
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            Entity pathBlobberEntity = SystemAPI.GetSingletonEntity<LittleDudePathBlobber>();
            LittleDudePathBlobber pathBlobber = SystemAPI.GetComponent<LittleDudePathBlobber>(pathBlobberEntity);

            new ResetJob
            {
                PathChunks = pathBlobber.PathBlob,
            }.Schedule().Complete();
            
            new PathTargetJob
            {
                PathChunks = pathBlobber.PathBlob,
                ChunkIndexToListIndex = pathBlobber.ChunkIndexToListIndex,
            }.Schedule();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
    
    [BurstCompile]
    public struct ResetJob : IJob
    {
        public BlobAssetReference<LittleDudePathChunkArray> PathChunks;

        [BurstCompile]
        public void Execute()
        {
            int chunksLength = PathChunks.Value.PathChunks.Length;
            for (int i = 0; i < chunksLength; i++)
            {
                ref LittleDudePathChunk valuePathChunk = ref PathChunks.Value.PathChunks[i];

                for (int j = 0; j < PathUtility.GRID_LENGTH; j++)
                {
                    valuePathChunk.TargetIndexes[j] = 0;
                }
            }
        }
    }
    
    [BurstCompile, WithNone(typeof(LittleDudeComponent))]
    public partial struct PathTargetJob : IJobEntity
    {
        public BlobAssetReference<LittleDudePathChunkArray> PathChunks;

        [ReadOnly, NativeDisableContainerSafetyRestriction]
        public NativeHashMap<int2, int>.ReadOnly ChunkIndexToListIndex;

        [BurstCompile]
        public void Execute(in LocalTransform transform, in FlowFieldComponent flowField)
        {
            ref LittleDudePathChunk valuePathChunk = ref PathChunks.Value.PathChunks[ChunkIndexToListIndex[flowField.PathIndex.ChunkIndex]];
            ref BlobArray<int> targets = ref valuePathChunk.TargetIndexes;
            targets[flowField.PathIndex.GridIndex] += flowField.Importance;
        }
    }
}
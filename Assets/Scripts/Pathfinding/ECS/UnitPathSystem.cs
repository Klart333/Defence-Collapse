using Effects.LittleDudes;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.Entities;
using Unity.Burst;
using Unity.Jobs;
using Enemy.ECS;

namespace Pathfinding.ECS
{
    public partial struct UnitPathSystem : ISystem
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

            new ResetJob
            {
                PathChunks = pathBlobber.PathBlob,
            }.Schedule().Complete();
            
            new UnitPathJob()
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
        public BlobAssetReference<PathChunkArray> PathChunks;

        [BurstCompile]
        public void Execute()
        {
            int chunksLength = PathChunks.Value.PathChunks.Length;
            for (int i = 0; i < chunksLength; i++)
            {
                ref PathChunk valuePathChunk = ref PathChunks.Value.PathChunks[i];

                for (int j = 0; j < PathUtility.GRID_LENGTH; j++)
                {
                    valuePathChunk.MovementCosts[j] -= valuePathChunk.Units[j] * 100000;
                    valuePathChunk.Units[j] = 0;
                }
            }
        }
    }
    
    [BurstCompile, WithNone(typeof(LittleDudeComponent))]
    public partial struct UnitPathJob : IJobEntity
    {
        public BlobAssetReference<PathChunkArray> PathChunks;

        [ReadOnly, NativeDisableContainerSafetyRestriction]
        public NativeHashMap<int2, int>.ReadOnly ChunkIndexToListIndex;

        [BurstCompile]
        public void Execute(in LocalTransform transform, in FlowFieldComponent flowField)
        {
            ref PathChunk valuePathChunk = ref PathChunks.Value.PathChunks[ChunkIndexToListIndex[flowField.PathIndex.ChunkIndex]];
            ref BlobArray<int> movementCosts = ref valuePathChunk.MovementCosts;
            if (movementCosts[flowField.PathIndex.GridIndex] < int.MaxValue)
            {
                movementCosts[flowField.PathIndex.GridIndex] += 100000;
            }
            
            ref BlobArray<int> units = ref valuePathChunk.Units;
            if (units[flowField.PathIndex.GridIndex] < short.MaxValue)
            {
                units[flowField.PathIndex.GridIndex] += 1;
            }
        }
    }
}
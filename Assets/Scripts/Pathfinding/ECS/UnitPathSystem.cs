using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.Entities;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

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
            int arrayLength = PathChunks.Value.PathChunks[0].Units.Length;
            for (int i = 0; i < chunksLength; i++)
            {
                for (int j = 0; j < arrayLength; j++)
                {
                    PathChunks.Value.PathChunks[i].MovementCosts[i] -= PathChunks.Value.PathChunks[i].Units[i];
                    PathChunks.Value.PathChunks[i].Units[j] = 0;
                }
            }
        }
    }
    
    [BurstCompile, WithAll(typeof(FlowFieldComponent))]
    public partial struct UnitPathJob : IJobEntity
    {
        public BlobAssetReference<PathChunkArray> PathChunks;

        [ReadOnly, NativeDisableContainerSafetyRestriction]
        public NativeHashMap<int2, int>.ReadOnly ChunkIndexToListIndex;

        [BurstCompile]
        public void Execute(in LocalTransform transform)
        {
            PathIndex index = PathManager.GetIndex(transform.Position.x, transform.Position.z);
            ref PathChunk valuePathChunk = ref PathChunks.Value.PathChunks[ChunkIndexToListIndex[index.ChunkIndex]];
            ref BlobArray<int> movementCosts = ref valuePathChunk.MovementCosts;
            if (movementCosts[index.GridIndex] < int.MaxValue)
            {
                movementCosts[index.GridIndex]++;
            }
            
            ref BlobArray<short> units = ref valuePathChunk.Units;
            if (units[index.GridIndex] < short.MaxValue)
            {
                units[index.GridIndex]++;
            }
        }
    }
}
using Unity.Mathematics;
using Unity.Collections;
using Pathfinding.ECS;
using Effects.ECS.ECB;
using Unity.Entities;
using Unity.Burst;
using Effects.ECS;
using Pathfinding;

namespace Enemy.ECS
{
    [BurstCompile, UpdateAfter(typeof(BeforeDeathECBSystem)), UpdateBefore(typeof(DeathSystem))]
    public partial struct ClusterCleanupSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PathBlobber>();
            EntityQuery query = SystemAPI.QueryBuilder().WithAll<EnemyClusterComponent, DeathTag>().Build();
            
            state.RequireForUpdate(query);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            PathBlobber pathBlobber = SystemAPI.GetSingleton<PathBlobber>();
            state.Dependency = new ClusterCleanupJob
            {
                ChunkIndexToListIndex = pathBlobber.ChunkIndexToListIndex,
                PathChunks = pathBlobber.PathBlob,
            }.Schedule(state.Dependency);
            state.Dependency.Complete();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
    
    [BurstCompile, WithAll(typeof(DeathTag))]
    public partial struct ClusterCleanupJob : IJobEntity
    {
        [ReadOnly]
        public BlobAssetReference<PathChunkArray> PathChunks;
        
        [ReadOnly]
        public NativeHashMap<int2, int>.ReadOnly ChunkIndexToListIndex;
        
        public void Execute(in EnemyClusterComponent cluster, in FlowFieldComponent flowField)
        {
            ref PathChunk valuePathChunk = ref PathChunks.Value.PathChunks[ChunkIndexToListIndex[flowField.PathIndex.ChunkIndex]];
            int combinedIndex = flowField.PathIndex.GridIndex.x + flowField.PathIndex.GridIndex.y * PathUtility.GRID_WIDTH;
            valuePathChunk.IndexOccupied[combinedIndex] = false;
        }
    }
}
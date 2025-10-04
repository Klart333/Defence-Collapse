using Unity.Mathematics;
using Unity.Collections;
using Pathfinding.ECS;
using Unity.Entities;
using Unity.Burst;
using Pathfinding;

namespace Enemy.ECS
{
    [BurstCompile]
    public partial struct FlowMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PathBlobber>();

            EntityQuery query = SystemAPI.QueryBuilder().WithAll<UpdateClusterPositionComponent>().WithNone<MovingClusterComponent>().Build();
            state.RequireForUpdate(query);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
            PathBlobber pathBlobber = SystemAPI.GetSingleton<PathBlobber>();
            
            state.Dependency = new FlowMovementJob
            {
                ChunkIndexToListIndex = pathBlobber.ChunkIndexToListIndex,
                PathChunks = pathBlobber.PathBlob,
                ECB = ecb,
            }.Schedule(state.Dependency);
            
            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }

    [BurstCompile, 
     WithNone(typeof(AttackingComponent), typeof(MovingClusterComponent))]
    public partial struct FlowMovementJob : IJobEntity
    {
        [ReadOnly]
        public BlobAssetReference<PathChunkArray> PathChunks;
        
        [ReadOnly]
        public NativeHashMap<int2, int>.ReadOnly ChunkIndexToListIndex;
        
        public EntityCommandBuffer ECB;
        
        [BurstCompile]
        private void Execute(Entity entity, ref FlowFieldComponent flowField, ref EnemyClusterComponent cluster, in SpeedComponent speed, ref UpdateClusterPositionComponent updateClusterComponent)
        {
            ref PathChunk valuePathChunk = ref PathChunks.Value.PathChunks[ChunkIndexToListIndex[flowField.PathIndex.ChunkIndex]];
            int combinedIndex = flowField.PathIndex.GridIndex.x + flowField.PathIndex.GridIndex.y * PathUtility.GRID_WIDTH;

            float2 direction = PathUtility.ByteToDirection(valuePathChunk.Directions[combinedIndex]);
            
            float3 movedPosition = cluster.Position + (math.round(direction) * PathUtility.CELL_SCALE).XyZ();
            PathIndex movedPathIndex = PathUtility.GetIndex(movedPosition.xz);
            int combinedMovedIndex = movedPathIndex.GridIndex.x + movedPathIndex.GridIndex.y * PathUtility.GRID_WIDTH;
            ref PathChunk movedValuePathChunk = ref PathChunks.Value.PathChunks[ChunkIndexToListIndex[movedPathIndex.ChunkIndex]];
            if (!ChunkIndexToListIndex.ContainsKey(movedPathIndex.ChunkIndex) || movedValuePathChunk.IndexOccupied[combinedMovedIndex]) return;

            valuePathChunk.IndexOccupied[combinedIndex] = false;
            
            movedValuePathChunk.IndexOccupied[combinedMovedIndex] = true;
            
            ECB.AddComponent(entity, new UpdatePositioningComponent
            {
                PreviousTile = flowField.PathIndex,
                CurrentTile = movedPathIndex,
            });
            
            flowField.PathIndex = movedPathIndex;
            cluster.Position = movedPosition;
            
            updateClusterComponent.Count--;
            if (updateClusterComponent.Count <= 0) ECB.RemoveComponent<UpdateClusterPositionComponent>(entity);
            ECB.AddComponent(entity, new MovingClusterComponent { TimeLeft = speed.Speed });

            ref PathChunk movedPathChunk = ref PathChunks.Value.PathChunks[ChunkIndexToListIndex[movedPathIndex.ChunkIndex]];
            float2 newDirection = PathUtility.ByteToDirection(movedPathChunk.Directions[combinedMovedIndex]);
            cluster.Facing = newDirection;
            
            float3 facingMovedPosition = movedPosition + (math.round(newDirection) * PathUtility.CELL_SCALE).XyZ();
            PathIndex facingPathIndex = PathUtility.GetIndex(facingMovedPosition.xz);
            cluster.TargetPathIndex = ChunkIndexToListIndex.ContainsKey(facingPathIndex.ChunkIndex) ? facingPathIndex : movedPathIndex;
        }
    }
}


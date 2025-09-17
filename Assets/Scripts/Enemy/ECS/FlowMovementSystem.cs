using Unity.Mathematics;
using Unity.Collections;
using Pathfinding.ECS;
using Unity.Entities;
using Unity.Burst;
using Pathfinding;

namespace Enemy.ECS
{
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
                ECB = ecb.AsParallelWriter(),
            }.ScheduleParallel(state.Dependency);
            
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
        
        public EntityCommandBuffer.ParallelWriter ECB;
        
        [BurstCompile]
        private void Execute([ChunkIndexInQuery] int sortKey, Entity entity, ref FlowFieldComponent flowField, ref EnemyClusterComponent cluster, in SpeedComponent speed, ref UpdateClusterPositionComponent updateClusterComponent)
        {
            ref PathChunk valuePathChunk = ref PathChunks.Value.PathChunks[ChunkIndexToListIndex[flowField.PathIndex.ChunkIndex]];
            float2 direction = PathUtility.ByteToDirection(valuePathChunk.Directions[flowField.PathIndex.GridIndex]);
            
            float3 movedPosition = cluster.Position + (math.round(direction) * PathUtility.CELL_SCALE).XyZ();
            PathIndex movedPathIndex = PathUtility.GetIndex(movedPosition.xz);
            if (!ChunkIndexToListIndex.ContainsKey(movedPathIndex.ChunkIndex)) return;
            
            flowField.PathIndex = movedPathIndex;
            cluster.Position = movedPosition;
            
            updateClusterComponent.Count--;
            if (updateClusterComponent.Count <= 0) ECB.RemoveComponent<UpdateClusterPositionComponent>(sortKey, entity);
            
            ECB.AddComponent<UpdatePositioningTag>(sortKey, entity);
            ECB.AddComponent(sortKey, entity, new MovingClusterComponent { TimeLeft = speed.Speed });

            ref PathChunk movedPathChunk = ref PathChunks.Value.PathChunks[ChunkIndexToListIndex[movedPathIndex.ChunkIndex]];
            float2 newDirection = PathUtility.ByteToDirection(movedPathChunk.Directions[movedPathIndex.GridIndex]);
            cluster.Facing = newDirection;
            
            float3 facingMovedPosition = movedPosition + (math.round(newDirection) * PathUtility.CELL_SCALE).XyZ();
            PathIndex facingPathIndex = PathUtility.GetIndex(facingMovedPosition.xz);
            cluster.TargetPathIndex = ChunkIndexToListIndex.ContainsKey(facingPathIndex.ChunkIndex) ? facingPathIndex : movedPathIndex;
        }
    }
}


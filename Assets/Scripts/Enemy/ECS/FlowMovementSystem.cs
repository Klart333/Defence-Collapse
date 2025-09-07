using Gameplay.Turns.ECS;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Pathfinding.ECS;
using Unity.Entities;
using Unity.Burst;
using Pathfinding;
using Utility;

namespace Enemy.ECS
{
    [UpdateInGroup(typeof(TransformSystemGroup))]
    public partial struct FlowMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TurnIncreaseComponent>();
            state.RequireForUpdate<PathBlobber>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            PathBlobber pathBlobber = SystemAPI.GetSingleton<PathBlobber>();
            TurnIncreaseComponent turnIncrease = SystemAPI.GetSingleton<TurnIncreaseComponent>();
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
            
            state.Dependency = new FlowMovementJob
            {
                ECB = ecb.AsParallelWriter(),
                PathChunks = pathBlobber.PathBlob,
                ChunkIndexToListIndex = pathBlobber.ChunkIndexToListIndex,
                TurnIncrease = turnIncrease.TurnIncrease,
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

    [BurstCompile]
    public partial struct FlowMovementJob : IJobEntity
    {
        [ReadOnly]
        public BlobAssetReference<PathChunkArray> PathChunks;
        
        [ReadOnly]
        public NativeHashMap<int2, int>.ReadOnly ChunkIndexToListIndex;

        public int TurnIncrease;

        public EntityCommandBuffer.ParallelWriter ECB;
        
        [BurstCompile]
        private void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in SpeedComponent speed, ref FlowFieldComponent flowField, ref EnemyClusterComponent cluster)
        {
            flowField.MoveTimer -= TurnIncrease;
            if (flowField.MoveTimer > 0) return;
            flowField.MoveTimer = (int)math.round(1.0f / speed.Speed);
            
            ref PathChunk valuePathChunk = ref PathChunks.Value.PathChunks[ChunkIndexToListIndex[flowField.PathIndex.ChunkIndex]];
            float2 direction = PathUtility.ByteToDirection(valuePathChunk.Directions[flowField.PathIndex.GridIndex]);
            
            float3 movedPosition = cluster.Position + new float3(Math.GetMultiple(direction.x, PathUtility.CELL_SCALE), 0, Math.GetMultiple(direction.y, PathUtility.CELL_SCALE)) * 2;
            PathIndex movedPathIndex = PathUtility.GetIndex(movedPosition.xz);
            if (!ChunkIndexToListIndex.ContainsKey(movedPathIndex.ChunkIndex)) return;
            
            flowField.PathIndex = movedPathIndex;
            cluster.Position = movedPosition;
            
            ref PathChunk movedPathChunk = ref PathChunks.Value.PathChunks[ChunkIndexToListIndex[movedPathIndex.ChunkIndex]];
            float2 newDirection = PathUtility.ByteToDirection(movedPathChunk.Directions[movedPathIndex.GridIndex]);
            cluster.Facing = newDirection;
            
            ECB.AddComponent<UpdatePositioningTag>(sortKey, entity);
        }
    }
}


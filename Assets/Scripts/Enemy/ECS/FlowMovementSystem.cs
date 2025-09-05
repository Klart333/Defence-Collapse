using Gameplay.Turns.ECS;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Pathfinding.ECS;
using Unity.Entities;
using Unity.Burst;
using Pathfinding;

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
            Entity pathBlobberEntity = SystemAPI.GetSingletonEntity<PathBlobber>();
            PathBlobber pathBlobber = SystemAPI.GetComponent<PathBlobber>(pathBlobberEntity);
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
            float3 direction = PathUtility.ByteToDirectionFloat3(valuePathChunk.Directions[flowField.PathIndex.GridIndex], 0);
            
            float3 movement = direction * speed.Speed;
            PathIndex movedPathIndex = PathUtility.GetIndex(cluster.Position.x + movement.x, cluster.Position.z + movement.z);
            if (!ChunkIndexToListIndex.ContainsKey(movedPathIndex.ChunkIndex)) return;
            
            flowField.PathIndex = movedPathIndex;
            cluster.Position += movement;
            
            ECB.AddComponent<UpdatePositioningTag>(sortKey, entity);
        }
    }
}


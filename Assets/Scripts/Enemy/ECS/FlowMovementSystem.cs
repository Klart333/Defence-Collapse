using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.Entities;
using Unity.Burst;
using Pathfinding;
using Gameplay;
using Pathfinding.ECS;

namespace DataStructures.Queue.ECS
{
    [UpdateInGroup(typeof(TransformSystemGroup))]
    public partial struct FlowMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PathBlobber>();
        }

        public void OnUpdate(ref SystemState state)
        {
            Entity pathBlobberEntity = SystemAPI.GetSingletonEntity<PathBlobber>();
            PathBlobber pathBlobber = SystemAPI.GetComponent<PathBlobber>(pathBlobberEntity);
            
            float deltaTime = SystemAPI.Time.DeltaTime;
            
            new FlowMovementJob
            {
                DeltaTime = deltaTime * GameSpeedManager.Instance.Value,
                PathChunks = pathBlobber.PathBlob,
                ChunkIndexToListIndex = pathBlobber.ChunkIndexToListIndex,
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }

    [BurstCompile, WithNone(typeof(AttackingComponent))]
    internal partial struct FlowMovementJob : IJobEntity
    {
        [ReadOnly]
        public float DeltaTime;
        
        public BlobAssetReference<PathChunkArray> PathChunks;
        
        [ReadOnly, NativeDisableContainerSafetyRestriction]
        public NativeHashMap<int2, int>.ReadOnly ChunkIndexToListIndex;

        [BurstCompile]
        private void Execute(in SpeedComponent speed, ref FlowFieldComponent flowField, ref LocalTransform transform)
        {
            PathIndex index = PathManager.GetIndex(transform.Position.x, transform.Position.z);
            ref PathChunk valuePathChunk = ref PathChunks.Value.PathChunks[ChunkIndexToListIndex[index.ChunkIndex]];
            float3 direction = PathManager.ByteToDirectionFloat3(valuePathChunk.Directions[index.GridIndex], flowField.Forward.y);
            
            flowField.Forward = math.normalize(flowField.Forward + direction * (flowField.TurnSpeed * DeltaTime));
            flowField.Up = math.normalize(flowField.Up + flowField.TargetUp * flowField.TurnSpeed * DeltaTime * 5);
            transform.Rotation = quaternion.LookRotation(flowField.Forward, flowField.Up);

            float3 movement = transform.Forward() * speed.Speed * DeltaTime;
            transform.Position += movement;
        }
    }
}


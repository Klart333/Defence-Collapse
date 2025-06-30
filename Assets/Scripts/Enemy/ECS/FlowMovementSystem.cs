using Effects.LittleDudes;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Pathfinding.ECS;
using Unity.Entities;
using Unity.Burst;
using Pathfinding;
using Gameplay;

namespace Enemy.ECS
{
    [UpdateInGroup(typeof(TransformSystemGroup)), UpdateAfter(typeof(GroundSystem))]
    public partial struct FlowMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameSpeedComponent>();
            state.RequireForUpdate<PathBlobber>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            Entity pathBlobberEntity = SystemAPI.GetSingletonEntity<PathBlobber>();
            PathBlobber pathBlobber = SystemAPI.GetComponent<PathBlobber>(pathBlobberEntity);
            
            float deltaTime = SystemAPI.Time.DeltaTime;
            float gameSpeed = SystemAPI.GetSingleton<GameSpeedComponent>().Speed;

            new FlowMovementJob
            {
                DeltaTime = deltaTime * gameSpeed,
                PathChunks = pathBlobber.PathBlob,
                ChunkIndexToListIndex = pathBlobber.ChunkIndexToListIndex,
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }

    [BurstCompile, WithNone(typeof(AttackingComponent), typeof(LittleDudeComponent))]
    internal partial struct FlowMovementJob : IJobEntity
    {
        [ReadOnly]
        public float DeltaTime;
        
        public BlobAssetReference<PathChunkArray> PathChunks;
        
        [ReadOnly]
        public NativeHashMap<int2, int>.ReadOnly ChunkIndexToListIndex;

        [BurstCompile]
        private void Execute(in SpeedComponent speed, ref FlowFieldComponent flowField, ref LocalTransform transform)
        {
            ref PathChunk valuePathChunk = ref PathChunks.Value.PathChunks[ChunkIndexToListIndex[flowField.PathIndex.ChunkIndex]];
            float3 direction = PathUtility.ByteToDirectionFloat3(valuePathChunk.Directions[flowField.PathIndex.GridIndex], flowField.Forward.y);
            
            flowField.Forward = math.normalize(flowField.Forward + direction * (flowField.TurnSpeed * DeltaTime));
            flowField.Up = math.normalize(flowField.Up + flowField.TargetUp * flowField.TurnSpeed * DeltaTime * 5);
            transform.Rotation = quaternion.LookRotation(flowField.Forward, flowField.Up);

            float3 movement = transform.Forward() * speed.Speed * DeltaTime;
            PathIndex movedPathIndex = PathUtility.GetIndex(transform.Position.x, transform.Position.z);
            if (!ChunkIndexToListIndex.ContainsKey(movedPathIndex.ChunkIndex))
            {
                return;
            }
            flowField.PathIndex = movedPathIndex;
            transform.Position += movement;
        }
    }
}


using Effects.ECS;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Pathfinding.ECS;
using Unity.Entities;
using Unity.Burst;
using Pathfinding;
using Enemy.ECS;
using Gameplay;

namespace Effects.LittleDudes
{
    [UpdateInGroup(typeof(TransformSystemGroup)), UpdateAfter(typeof(GroundSystem))] 
    public partial struct LittleDudeFlowMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameSpeedComponent>(); 
            state.RequireForUpdate<LittleDudePathBlobber>();

            EntityQuery littleDudeQuery = SystemAPI.QueryBuilder().WithAll<LittleDudeComponent>().Build();
            state.RequireForUpdate(littleDudeQuery);        
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            Entity pathBlobberEntity = SystemAPI.GetSingletonEntity<LittleDudePathBlobber>();
            LittleDudePathBlobber pathBlobber = SystemAPI.GetComponent<LittleDudePathBlobber>(pathBlobberEntity);
            
            float deltaTime = SystemAPI.Time.DeltaTime;
            float gameSpeed = SystemAPI.GetSingleton<GameSpeedComponent>().Speed;

            new LittleDudeFlowMovementJob
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

    [BurstCompile, WithNone(typeof(AttackingComponent)), WithAll(typeof(LittleDudeComponent))]
    internal partial struct LittleDudeFlowMovementJob : IJobEntity
    {
        [ReadOnly]
        public float DeltaTime;
        
        public BlobAssetReference<LittleDudePathChunkArray> PathChunks;
        
        [ReadOnly]
        public NativeHashMap<int2, int>.ReadOnly ChunkIndexToListIndex;

        [BurstCompile]
        private void Execute(in SpeedComponent speed, ref FlowFieldComponent flowField, ref LocalTransform transform)
        {
            ref LittleDudePathChunk valuePathChunk = ref PathChunks.Value.PathChunks[ChunkIndexToListIndex[flowField.PathIndex.ChunkIndex]];
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
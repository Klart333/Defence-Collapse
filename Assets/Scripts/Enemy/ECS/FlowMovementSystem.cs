using Pathfinding;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.Entities;
using Unity.Burst;

namespace DataStructures.Queue.ECS
{
    [UpdateInGroup(typeof(TransformSystemGroup))]
    public partial struct FlowMovementSystem : ISystem
    {
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            
        }

        public void OnUpdate(ref SystemState state)
        {
            if (PathManager.Instance == null)
            {
                return;
            }
            
            float deltaTime = SystemAPI.Time.DeltaTime;
            
            new FlowMovementJob()
            {
                DeltaTime = deltaTime,
                CellScale = PathManager.Instance.CellScale,
                GridWidth = PathManager.Instance.GridWidth,
                Directions = PathManager.Instance.Directions.AsReadOnly(),
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }

    [BurstCompile]
    internal partial struct FlowMovementJob : IJobEntity
    {
        [ReadOnly]
        public float DeltaTime;

        [ReadOnly]
        public float CellScale;

        [ReadOnly]
        public int GridWidth;
        
        [ReadOnly, NativeDisableContainerSafetyRestriction]
        public NativeArray<byte>.ReadOnly Directions;

        [BurstCompile]
        private void Execute(in SpeedComponent speed, ref FlowFieldComponent flowField, ref LocalTransform transform)
        {
            int index = PathManager.GetIndex(transform.Position.x, transform.Position.z, CellScale, GridWidth);
            float3 direction = PathManager.ByteToDirectionFloat3(Directions[index], flowField.Forward.y);
            
            flowField.Forward = math.normalize(flowField.Forward + direction * (flowField.TurnSpeed * DeltaTime));
            flowField.Up = math.normalize(flowField.Up + flowField.TargetUp * flowField.TurnSpeed * DeltaTime * 5);
            transform.Rotation = quaternion.LookRotation(flowField.Forward, flowField.Up);
            
            transform.Position += transform.Forward() * speed.Speed * DeltaTime;
        }
    }
}


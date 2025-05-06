using System.Numerics;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace InputCamera.ECS
{
    public partial struct RotateTowardCameraSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CameraPositionComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float3 camPos = SystemAPI.GetSingleton<CameraPositionComponent>().Position;

            new PositionJob
            {
                CameraPosition = camPos,
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
    
    [BurstCompile, WithAll(typeof(RotateTowardCameraTag))]
    public partial struct PositionJob : IJobEntity
    {
        public float3 CameraPosition;
        
        [BurstCompile]
        public void Execute(ref LocalTransform transform)
        {
            float3 dir = math.normalize(transform.Position - CameraPosition);
            transform.Rotation = quaternion.LookRotation(dir, transform.Up());
        }
    }
}
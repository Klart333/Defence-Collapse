using Unity.Transforms;
using Unity.Entities;
using Unity.Burst;
using Gameplay;
using Unity.Mathematics;

namespace Juice.Ecs
{
    public partial struct RotationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameSpeedComponent>();
        }

        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float gameSpeed = SystemAPI.GetSingleton<GameSpeedComponent>().Speed;
            float deltaTime = SystemAPI.Time.DeltaTime;

            new RotationJob
            {
                DeltaTime = deltaTime * gameSpeed,
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
    
    [BurstCompile]
    public partial struct RotationJob : IJobEntity
    {
        public float DeltaTime;
        
        [BurstCompile]
        public void Execute(ref LocalTransform transform, ref RotationComponent rotationComponent)
        {
            rotationComponent.Timer += DeltaTime;
            transform.Rotation = quaternion.AxisAngle(transform.Forward(), rotationComponent.TargetRotation * rotationComponent.Timer / rotationComponent.Duration);
        }
    }
    
    public struct RotationComponent : IComponentData
    {
        public float TargetRotation;
        public float Duration;
        public float Timer;
    }
}
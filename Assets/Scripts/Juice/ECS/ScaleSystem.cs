using Gameplay;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Juice.Ecs
{
    public partial struct ScaleSystem : ISystem
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

            new ScaleJob
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
    public partial struct ScaleJob : IJobEntity
    {
        public float DeltaTime;
        
        [BurstCompile]
        public void Execute(ref LocalTransform transform, ref ScaleComponent scaleComponent)
        {
            scaleComponent.Timer += DeltaTime;
            transform.Scale = math.lerp(scaleComponent.StartScale, scaleComponent.TargetScale, scaleComponent.Timer / scaleComponent.Duration);
        }
    }

    
    public struct ScaleComponent : IComponentData
    {
        public float TargetScale;
        public float StartScale;
        public float Duration;
        public float Timer;
    }
}
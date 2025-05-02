using Unity.Entities;
using Effects.ECS;
using Unity.Burst;
using Gameplay;

namespace DataStructures.Queue.ECS
{
    [UpdateAfter(typeof(HealthSystem))]
    public partial struct FresnelSystem : ISystem
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
            float deltaTime = SystemAPI.Time.DeltaTime * gameSpeed;

            new FresnelJob
            {
                FresnelIncrease = deltaTime * 10,
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }

    [BurstCompile]
    public partial struct FresnelJob : IJobEntity
    {
        public float FresnelIncrease;

        [BurstCompile]
        public void Execute(ref FresnelComponent fresnel)
        {
            if (fresnel.Value >= 10)
            {
                return;
            }

            fresnel.Value += FresnelIncrease;
        }
    }
}
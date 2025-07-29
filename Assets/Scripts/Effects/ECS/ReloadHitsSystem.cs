using Gameplay;
using Unity.Burst;
using Unity.Entities;

namespace Effects.ECS
{
    public partial struct ReloadHitsSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameSpeedComponent>(); 
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        { 
            float deltaTime = SystemAPI.Time.DeltaTime;
            float gameSpeed = SystemAPI.GetSingleton<GameSpeedComponent>().Speed;

            state.Dependency = new ReloadHitsJob
            {
                DeltaTime = deltaTime * gameSpeed,
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
    
    [BurstCompile]
    public partial struct ReloadHitsJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(ref ReloadHitsComponent reload, ref DamageComponent damage)
        {
            if (damage.LimitedHits >= reload.MaxHitAmount) return;
            
            reload.Timer += DeltaTime;

            if (reload.Timer < reload.ReloadInterval) return;
            
            damage.LimitedHits += 1;
            reload.Timer = 0;
        }
    }

    public struct ReloadHitsComponent : IComponentData
    {
        public int MaxHitAmount;
        public float Timer;
        public float ReloadInterval;
    }
}
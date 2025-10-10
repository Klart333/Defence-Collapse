using Effects.ECS.ECB;
using Unity.Entities;
using Effects.ECS;
using Unity.Burst;

namespace VFX.ECS
{
    [BurstCompile, UpdateBefore(typeof(BeforeDeathECBSystem))]
    public partial struct KillFireParticlesSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<FireParticlesComponent, DeathTag>().Build());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            VFXFireParticlesSingleton VFXFireParticlesSingleton = SystemAPI.GetSingletonRW<VFXFireParticlesSingleton>().ValueRW;

            new DeathFireParticlesVFXJob
            {
                FireParticlesManager = VFXFireParticlesSingleton.Manager,
            }.Schedule();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
        
        [BurstCompile, WithAll(typeof(DeathTag))]
        public partial struct DeathFireParticlesVFXJob : IJobEntity
        {
            public VFXManagerParented<VFXFireData> FireParticlesManager;
        
            private void Execute(in FireParticlesComponent fire)
            {
                FireParticlesManager.Kill(fire.FireParticleVFXIndex);
            }
        }
    }
}
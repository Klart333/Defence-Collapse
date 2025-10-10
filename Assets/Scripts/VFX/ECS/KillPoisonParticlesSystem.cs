using Effects.ECS.ECB;
using Unity.Entities;
using Effects.ECS;
using Unity.Burst;

namespace VFX.ECS
{
    [BurstCompile, UpdateBefore(typeof(BeforeDeathECBSystem))]
    public partial struct KillPoisonParticlesSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<PoisonParticlesComponent, DeathTag>().Build());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            VFXPoisonParticlesSingleton VFXPoisonParticlesSingleton = SystemAPI.GetSingletonRW<VFXPoisonParticlesSingleton>().ValueRW;

            new DeathPoisonParticlesVFXJob
            {
                PoisonParticlesManager = VFXPoisonParticlesSingleton.Manager,
            }.Schedule();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
        
        
        [BurstCompile, WithAll(typeof(DeathTag))]
        public partial struct DeathPoisonParticlesVFXJob : IJobEntity
        {
            public VFXManagerParented<VFXPoisonParticleData> PoisonParticlesManager;
        
            private void Execute(in PoisonParticlesComponent poison)
            {
                PoisonParticlesManager.Kill(poison.PoisonParticleVFXIndex);
            }
        }

    }
}
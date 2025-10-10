using Effects.ECS.ECB;
using Unity.Entities;
using Effects.ECS;
using Unity.Burst;

namespace VFX.ECS
{
    [BurstCompile, UpdateBefore(typeof(BeforeDeathECBSystem))]
    public partial struct KillLightningTrailSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<LightningTrailComponent, DeathTag>().Build());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            VFXLightningTrailSingleton vfxLightningTrailSingleton = SystemAPI.GetSingletonRW<VFXLightningTrailSingleton>().ValueRW;

            new DeathLightningTrailVFXJob
            { 
                LightningTrailManager = vfxLightningTrailSingleton.Manager,
            }.Schedule();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
        
        [BurstCompile, WithAll(typeof(DeathTag))]
        public partial struct DeathLightningTrailVFXJob : IJobEntity
        {
            public VFXManagerParented<VFXLightningTrailData> LightningTrailManager;
        
            private void Execute(in LightningTrailComponent trail)
            {
                LightningTrailManager.Kill(trail.LightningTrailVFXIndex);
            }
        }

    }
}
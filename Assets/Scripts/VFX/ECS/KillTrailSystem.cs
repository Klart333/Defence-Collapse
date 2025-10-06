using Effects.ECS.ECB;
using Unity.Entities;
using Effects.ECS;
using Unity.Burst;

namespace VFX.ECS
{
    [BurstCompile, UpdateBefore(typeof(BeforeDeathECBSystem))]
    public partial struct KillTrailSystem : ISystem
    {
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<TrailComponent, DeathTag>().Build());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            VFXTrailSingleton vfxTrailSingleton = SystemAPI.GetSingletonRW<VFXTrailSingleton>().ValueRW;

            new DeathTrailVFXJob
            {
                TrailManager = vfxTrailSingleton.Manager,
            }.Schedule();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    
        [BurstCompile, WithAll(typeof(DeathTag))]
        public partial struct DeathTrailVFXJob : IJobEntity
        {
            public VFXManagerParented<VFXTrailData> TrailManager;
        
            private void Execute(in TrailComponent trail)
            {
                TrailManager.Kill(trail.TrailVFXIndex);
            }
        }
    }
}
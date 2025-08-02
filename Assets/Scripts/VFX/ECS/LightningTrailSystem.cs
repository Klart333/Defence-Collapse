using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Entities;
using Unity.Burst;
using Effects.ECS;
using Enemy.ECS;

namespace VFX.ECS
{
    [BurstCompile, UpdateAfter(typeof(HealthSystem))]
    public partial struct LightningTrailSystem : ISystem
    {
        private EntityQuery initLightningTrailQuery;
        private EntityQuery deathLightningTrailQuery;
    
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            deathLightningTrailQuery = SystemAPI.QueryBuilder().WithAll<LightningTrailComponent, DeathTag>().Build();
            initLightningTrailQuery = SystemAPI.QueryBuilder().WithAll<LightningComponent, SpeedComponent>().WithNone<LightningTrailComponent>().Build();
            
            state.RequireForUpdate(SystemAPI.QueryBuilder().WithAny<LightningComponent, LightningTrailComponent>().Build());
            state.RequireForUpdate<VFXLightningTrailSingleton>(); 
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            VFXLightningTrailSingleton vfxLightningTrailSingleton = SystemAPI.GetSingletonRW<VFXLightningTrailSingleton>().ValueRW;
            if (!initLightningTrailQuery.IsEmpty)
            {
                InitializeLightningTrails(ref state, vfxLightningTrailSingleton);
            }

            state.Dependency = new SetLightningTrailVFXDataJob()
            {
                LightningTrailsData = vfxLightningTrailSingleton.Manager.Datas,
            }.ScheduleParallel(state.Dependency);
            
            if (!deathLightningTrailQuery.IsEmpty)
            {
                KillLightningTrails(ref state, vfxLightningTrailSingleton);
            }
        }

        private void InitializeLightningTrails(ref SystemState state, VFXLightningTrailSingleton trailSingleton)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

            state.Dependency = new InitializeLightningTrailVFXJob
            { 
                ECB = ecb,
                LightningTrailManager = trailSingleton.Manager,
            }.Schedule(state.Dependency);
                
            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
        
        private void KillLightningTrails(ref SystemState state, VFXLightningTrailSingleton trailSingleton)
        {
            state.Dependency = new DeathLightningTrailVFXJob
            {
                LightningTrailManager = trailSingleton.Manager,
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
    
    [BurstCompile, WithAll(typeof(LightningComponent), typeof(SpeedComponent)), WithNone(typeof(LightningTrailComponent))]
    public partial struct InitializeLightningTrailVFXJob : IJobEntity
    {
        public VFXManagerParented<VFXLightningTrailData> LightningTrailManager;
        public EntityCommandBuffer ECB;
        
        private void Execute(Entity entity, in LocalTransform transform)
        {
            LightningTrailComponent trail = new LightningTrailComponent
            {
                LightningTrailVFXIndex = LightningTrailManager.Create(),
            };
                
            ECB.AddComponent(entity, trail);
            LightningTrailManager.Datas[trail.LightningTrailVFXIndex] = new VFXLightningTrailData
            {
                Size = transform.Scale
            };
        }
    }
    
    [BurstCompile]
    public partial struct SetLightningTrailVFXDataJob : IJobEntity
    {
        [NativeDisableParallelForRestriction]
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<VFXLightningTrailData> LightningTrailsData;
            
        private void Execute(in LocalTransform transform, in LightningTrailComponent trail, in SpeedComponent speed)
        {
            if (trail.LightningTrailVFXIndex < 0) return;
            
            VFXLightningTrailData trailData = LightningTrailsData[trail.LightningTrailVFXIndex];
            trailData.Position = transform.Position;
            trailData.Velocity = math.mul(transform.Rotation, math.forward()) * speed.Speed;
            LightningTrailsData[trail.LightningTrailVFXIndex] = trailData;
        }
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

    public struct LightningTrailComponent : IComponentData
    {
        public int LightningTrailVFXIndex;
    }
}
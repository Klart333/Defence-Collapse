using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.Entities;
using Unity.Burst;
using Effects.ECS;

namespace VFX.ECS
{
    [BurstCompile, UpdateAfter(typeof(HealthSystem))]
    public partial struct TrailSystem : ISystem
    {
        private EntityQuery initTrailQuery;
    
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            initTrailQuery = SystemAPI.QueryBuilder().WithAll<InitTrailComponent>().Build();
            
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            VFXTrailSingleton vfxTrailSingleton = SystemAPI.GetSingletonRW<VFXTrailSingleton>().ValueRW;
            if (!initTrailQuery.IsEmpty)
            {
                InitializeTrails(ref state, vfxTrailSingleton);
            }

            state.Dependency = new SetTrailVFXDataJob
            {
                TrailsData = vfxTrailSingleton.Manager.Datas,
            }.ScheduleParallel(state.Dependency);
        }

        private void InitializeTrails(ref SystemState state, VFXTrailSingleton trailSingleton)
        {
            EndSimulationEntityCommandBufferSystem.Singleton singleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = singleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            new InitializeTrailVFXJob
            { 
                ECB = ecb,
                TrailManager = trailSingleton.Manager,
            }.Schedule();
        }
        
        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
    
    [BurstCompile]
    public partial struct InitializeTrailVFXJob : IJobEntity
    {
        public VFXManagerParented<VFXTrailData> TrailManager;
        public EntityCommandBuffer ECB;
        
        private void Execute(Entity entity, in LocalTransform transform, in InitTrailComponent initTrail)
        {
            TrailComponent trail = new TrailComponent
            {
                TrailVFXIndex = TrailManager.Create(),
            };
            ECB.AddComponent(entity, trail);
                
            TrailManager.Datas[trail.TrailVFXIndex] = new VFXTrailData
            {
                Color = new float3(1, 1, 1),
                Size = 0.2f * transform.Scale * initTrail.ScaleFactor,
                Length = 0.3f * transform.Scale * initTrail.ScaleFactor,
            };
            
            ECB.RemoveComponent<InitTrailComponent>(entity);
        }
    }
    
    [BurstCompile]
    public partial struct SetTrailVFXDataJob : IJobEntity
    {
        [NativeDisableParallelForRestriction]
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<VFXTrailData> TrailsData;
            
        private void Execute(in LocalTransform transform, in TrailComponent trail)
        {
            if (trail.TrailVFXIndex < 0) return;
            
            VFXTrailData trailData = TrailsData[trail.TrailVFXIndex];
            trailData.Position = transform.Position;
            trailData.Direction = math.mul(transform.Rotation, -math.forward());
            TrailsData[trail.TrailVFXIndex] = trailData;
        }
    }

    public struct TrailComponent : IComponentData
    {
        public int TrailVFXIndex;
    }
    
    public struct InitTrailComponent : IComponentData
    {
        public float ScaleFactor;
    }
}
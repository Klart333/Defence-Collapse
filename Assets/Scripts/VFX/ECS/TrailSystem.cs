using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.Entities;
using Unity.Burst;
using Effects.ECS;

namespace VFX.ECS
{
    [BurstCompile]
    public partial struct TrailSystem : ISystem
    {
        private EntityQuery initTrailQuery;
        private EntityQuery deathTrailQuery;
    
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            EntityQueryBuilder builder = new EntityQueryBuilder(state.WorldUpdateAllocator).WithAll<InitTrailComponent>();
            initTrailQuery = state.GetEntityQuery(builder);
            
            EntityQueryBuilder builder2 = new EntityQueryBuilder(state.WorldUpdateAllocator).WithAll<TrailComponent, DeathTag>();
            deathTrailQuery = state.GetEntityQuery(builder2);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            VFXTrailSingleton vfxTrailSingleton = SystemAPI.GetSingletonRW<VFXTrailSingleton>().ValueRW;
            if (!initTrailQuery.IsEmpty)
            {
                InitializeTrails(ref state, vfxTrailSingleton);
            }

            state.Dependency = new SetTrailVFXDataJob()
            {
                TrailsData = vfxTrailSingleton.Manager.Datas,
            }.ScheduleParallel(state.Dependency);
            
            if (!deathTrailQuery.IsEmpty)
            {
                KillTrails(ref state, vfxTrailSingleton);
            }
        }

        private void InitializeTrails(ref SystemState state, VFXTrailSingleton trailSingleton)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

            state.Dependency = new InitializeTrailVFXJob
            { 
                ECB = ecb,
                TrailManager = trailSingleton.Manager,
            }.Schedule(state.Dependency);
                
            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
        
        private void KillTrails(ref SystemState state, VFXTrailSingleton trailSingleton)
        {
            state.Dependency = new DeathTrailVFXJob
            {
                TrailManager = trailSingleton.Manager,
            }.Schedule(state.Dependency);
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
    
    [BurstCompile, WithAll(typeof(DeathTag))]
    public partial struct DeathTrailVFXJob : IJobEntity
    {
        public VFXManagerParented<VFXTrailData> TrailManager;
        
        private void Execute(in TrailComponent trail)
        {
            TrailManager.Kill(trail.TrailVFXIndex);
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
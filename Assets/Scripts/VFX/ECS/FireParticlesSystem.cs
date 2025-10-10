using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.Entities;
using Unity.Burst;
using Effects.ECS;
using Enemy.ECS;

namespace VFX.ECS
{
    [BurstCompile, UpdateAfter(typeof(HealthSystem))]
    public partial struct FireParticlesSystem : ISystem
    {
        private EntityQuery initQuery;
        private EntityQuery stoppedBurningQuery;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            initQuery = SystemAPI.QueryBuilder().WithAll<FireComponent, SpeedComponent>().WithNone<FireParticlesComponent>().Build();
            stoppedBurningQuery = SystemAPI.QueryBuilder().WithAll<FireParticlesComponent>().WithNone<FireComponent>().Build();
            
            state.RequireForUpdate(SystemAPI.QueryBuilder().WithAny<FireComponent, FireParticlesComponent>().Build());
            state.RequireForUpdate<VFXFireParticlesSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            VFXFireParticlesSingleton VFXFireParticlesSingleton = SystemAPI.GetSingletonRW<VFXFireParticlesSingleton>().ValueRW;
            if (!initQuery.IsEmpty)
            {
                InitializeFireParticles(ref state, VFXFireParticlesSingleton);
            }

            state.Dependency = new SetFireParticlesVFXDataJob()
            {
                FireData = VFXFireParticlesSingleton.Manager.Datas,
            }.ScheduleParallel(state.Dependency);
            
            if (!stoppedBurningQuery.IsEmpty)
            {
                StopBurningParticles(ref state, VFXFireParticlesSingleton);
            }
        }

        private void InitializeFireParticles(ref SystemState state, VFXFireParticlesSingleton fireSingleton)
        {
            var singleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = singleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            new InitializeFireParticlesVFXJob
            { 
                ECB = ecb,
                FireParticlesManager = fireSingleton.Manager,
            }.Schedule();
        }
        
        private void StopBurningParticles(ref SystemState state, VFXFireParticlesSingleton fireSingleton)
        {
            var singleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = singleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            new StoppedBurningFireParticlesVFXJob
            {
                FireParticlesManager = fireSingleton.Manager,
                ECB = ecb,
            }.Schedule();
        }
        
        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
    
    [BurstCompile, WithAll(typeof(FireComponent), typeof(SpeedComponent)), WithNone(typeof(FireParticlesComponent))]
    public partial struct InitializeFireParticlesVFXJob : IJobEntity
    {
        public VFXManagerParented<VFXFireData> FireParticlesManager;
        public EntityCommandBuffer ECB;
        
        private void Execute(Entity entity, in LocalTransform transform)
        {
            FireParticlesComponent fireParticle = new FireParticlesComponent
            {
                FireParticleVFXIndex = FireParticlesManager.Create(),
            };
            ECB.AddComponent(entity, fireParticle);
                
            FireParticlesManager.Datas[fireParticle.FireParticleVFXIndex] = new VFXFireData
            {
                Size = 1 * transform.Scale,
            };
        }
    }
    
    [BurstCompile]
    public partial struct SetFireParticlesVFXDataJob : IJobEntity
    {
        [NativeDisableParallelForRestriction]
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<VFXFireData> FireData;
            
        private void Execute(in LocalTransform transform, in FireParticlesComponent fire, in SpeedComponent speed)
        {
            if (fire.FireParticleVFXIndex < 0) return;
            
            VFXFireData fireData = FireData[fire.FireParticleVFXIndex];
            fireData.Position = transform.Position;
            fireData.Velocity = math.mul(transform.Rotation, math.forward()) * speed.Speed;
            FireData[fire.FireParticleVFXIndex] = fireData;
        }
    }
    
    [BurstCompile, WithNone(typeof(FireComponent))]
    public partial struct StoppedBurningFireParticlesVFXJob : IJobEntity
    {
        public VFXManagerParented<VFXFireData> FireParticlesManager;
        public EntityCommandBuffer ECB;
        
        private void Execute(in FireParticlesComponent fire, Entity entity)
        {
            FireParticlesManager.Kill(fire.FireParticleVFXIndex);
            
            ECB.RemoveComponent<FireParticlesComponent>(entity);
        }
    }
    
    public struct FireParticlesComponent : IComponentData
    {
        public int FireParticleVFXIndex;
    }
}
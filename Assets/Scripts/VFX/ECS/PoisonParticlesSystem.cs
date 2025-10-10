using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.Entities;
using Unity.Burst;
using Effects.ECS;
using Enemy.ECS;
using VFX.ECS;
using VFX;

namespace VFX.ECS
{
    [BurstCompile, UpdateAfter(typeof(HealthSystem))]
    public partial struct PoisonParticleSytem : ISystem
    {
        private EntityQuery stoppedQuery;
        private EntityQuery initQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            initQuery = SystemAPI.QueryBuilder().WithAll<PoisonComponent, SpeedComponent>().WithNone<PoisonParticlesComponent>().Build();
            stoppedQuery = SystemAPI.QueryBuilder().WithAll<PoisonParticlesComponent>().WithNone<PoisonComponent>().Build();

            state.RequireForUpdate(SystemAPI.QueryBuilder().WithAny<PoisonComponent, PoisonParticlesComponent>().Build());
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<VFXPoisonParticlesSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            VFXPoisonParticlesSingleton VFXPoisonParticlesSingleton = SystemAPI.GetSingletonRW<VFXPoisonParticlesSingleton>().ValueRW;
            if (!initQuery.IsEmpty)
            {
                InitializePoisonParticles(ref state, VFXPoisonParticlesSingleton);
            }

            state.Dependency = new SetPoisonParticlesVFXDataJob()
            {
                PoisonData = VFXPoisonParticlesSingleton.Manager.Datas,
            }.ScheduleParallel(state.Dependency);

            if (!stoppedQuery.IsEmpty)
            {
                StopBurningParticles(ref state, VFXPoisonParticlesSingleton);
            }
        }

        private void InitializePoisonParticles(ref SystemState state, VFXPoisonParticlesSingleton poisonSingleton)
        {
            var singleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = singleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            new InitializePoisonParticlesVFXJob
            {
                ECB = ecb,
                PoisonParticlesManager = poisonSingleton.Manager,
            }.Schedule();
        }

        private void StopBurningParticles(ref SystemState state, VFXPoisonParticlesSingleton poisonSingleton)
        {
            var singleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = singleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            new StoppedPoisonParticlesVFXJob
            {
                PoisonParticlesManager = poisonSingleton.Manager,
                ECB = ecb,
            }.Schedule();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }

        [BurstCompile, WithAll(typeof(PoisonComponent), typeof(SpeedComponent)), WithNone(typeof(PoisonParticlesComponent))]
        public partial struct InitializePoisonParticlesVFXJob : IJobEntity
        {
            public VFXManagerParented<VFXPoisonParticleData> PoisonParticlesManager;
            public EntityCommandBuffer ECB;

            private void Execute(Entity entity, in LocalTransform transform)
            {
                PoisonParticlesComponent poisonParticle = new PoisonParticlesComponent
                {
                    PoisonParticleVFXIndex = PoisonParticlesManager.Create(),
                };
                ECB.AddComponent(entity, poisonParticle);

                PoisonParticlesManager.Datas[poisonParticle.PoisonParticleVFXIndex] = new VFXPoisonParticleData
                {
                    Size = 1 * transform.Scale,
                };
            }
        }

        [BurstCompile]
        public partial struct SetPoisonParticlesVFXDataJob : IJobEntity
        {
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<VFXPoisonParticleData> PoisonData;

            private void Execute(in LocalTransform transform, in PoisonParticlesComponent poison, in SpeedComponent speed)
            {
                if (poison.PoisonParticleVFXIndex < 0) return;

                VFXPoisonParticleData poisonData = PoisonData[poison.PoisonParticleVFXIndex];
                poisonData.Position = transform.Position;
                poisonData.Velocity = math.mul(transform.Rotation, math.forward()) * speed.Speed;
                PoisonData[poison.PoisonParticleVFXIndex] = poisonData;
            }
        }

        [BurstCompile, WithNone(typeof(PoisonComponent))]
        public partial struct StoppedPoisonParticlesVFXJob : IJobEntity
        {
            public VFXManagerParented<VFXPoisonParticleData> PoisonParticlesManager;
            public EntityCommandBuffer ECB;

            private void Execute(in PoisonParticlesComponent poison, Entity entity)
            {
                PoisonParticlesManager.Kill(poison.PoisonParticleVFXIndex);

                ECB.RemoveComponent<PoisonParticlesComponent>(entity);
            }
        }
    }
    
    public struct PoisonParticlesComponent : IComponentData
    {
        public int PoisonParticleVFXIndex;
    }
}
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.Entities;
using Unity.Burst;

namespace Effects.ECS
{
    [BurstCompile, UpdateAfter(typeof(HealthSystem))]
    public partial struct PunchScalerSystem : ISystem
    {
        private ComponentLookup<PunchScaleComponent> PunchScaleLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<DamageTakenComponent>();
            
            PunchScaleLookup = SystemAPI.GetComponentLookup<PunchScaleComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            PunchScaleLookup.Update(ref state);
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            new PunchScalerJob
            {
                ECB = ecb.AsParallelWriter(),
                PunchScaleLookup = PunchScaleLookup,
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            
        }
    }

    [BurstCompile, WithNone(typeof(DeathTag))]
    public partial struct PunchScalerJob : IJobEntity
    {
        [ReadOnly]
        public ComponentLookup<PunchScaleComponent> PunchScaleLookup;

        public EntityCommandBuffer.ParallelWriter ECB;

        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in DamageTakenComponent damageTakenComponent, in LocalTransform transform)
        {
            if (PunchScaleLookup.TryGetComponent(entity, out PunchScaleComponent punchScale))
            {
                float damage = damageTakenComponent.DamageTaken + punchScale.Damage;
                float scaleMult = 1.0f + math.log10(damage) / 8.0f;
                ECB.SetComponent(sortKey, entity, new PunchScaleComponent
                {
                    Duration = punchScale.Duration + 0.05f,
                    StartScale = punchScale.StartScale,
                    PunchScale = punchScale.StartScale * scaleMult,
                    Damage = damage
                });
            }
            else
            {
                float scaleMult = 1.0f + math.log10(damageTakenComponent.DamageTaken) / 8.0f;
                ECB.AddComponent(sortKey, entity, new PunchScaleComponent
                {
                    Duration = 0.2f,
                    StartScale = transform.Scale,
                    PunchScale = transform.Scale * scaleMult,
                    Damage = damageTakenComponent.DamageTaken,
                });
            }
        }
    }
}
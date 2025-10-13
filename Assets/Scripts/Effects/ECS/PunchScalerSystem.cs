using Enemy.ECS;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.Entities;
using Unity.Burst;
using UnityEngine;

namespace Effects.ECS
{
    [BurstCompile, UpdateAfter(typeof(HealthSystem))]
    public partial struct PunchScalerSystem : ISystem
    {
        private ComponentLookup<PunchScaleComponent> PunchScaleLookup;
        private BufferLookup<DamageTakenBuffer> damageTakenBufferLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            damageTakenBufferLookup = SystemAPI.GetBufferLookup<DamageTakenBuffer>();
            PunchScaleLookup = SystemAPI.GetComponentLookup<PunchScaleComponent>();
            
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<DamageTakenTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            damageTakenBufferLookup.Update(ref state);
            PunchScaleLookup.Update(ref state);

            new PunchScalerJob
            {
                DamageTakenBufferLookup = damageTakenBufferLookup,
                PunchScaleLookup = PunchScaleLookup,
                ECB = ecb.AsParallelWriter(),
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            
        }
    }

    [BurstCompile, WithAll(typeof(DamageTakenTag)), WithNone(typeof(DeathTag))]
    public partial struct PunchScalerJob : IJobEntity
    {
        [ReadOnly]
        public ComponentLookup<PunchScaleComponent> PunchScaleLookup;
        
        [ReadOnly]
        public BufferLookup<DamageTakenBuffer> DamageTakenBufferLookup;

        public EntityCommandBuffer.ParallelWriter ECB;

        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in LocalTransform transform)
        {
            DynamicBuffer<DamageTakenBuffer> damageTakenBuffer = DamageTakenBufferLookup[entity];

            float totalDamage = 0;
            for (int i = 0; i < damageTakenBuffer.Length; i++)
            {
                totalDamage += damageTakenBuffer[i].DamageTaken;
            }
            
            if (PunchScaleLookup.TryGetComponent(entity, out PunchScaleComponent punchScale))
            {
                float damage = totalDamage + punchScale.Damage;
                float scaleMult = 1.0f + math.log10(damage) / 8.0f;
                ECB.SetComponent(sortKey, entity, new PunchScaleComponent
                {
                    Duration = punchScale.Duration + 0.03f,
                    StartScale = punchScale.StartScale,
                    PunchScale = punchScale.StartScale * scaleMult,
                    Value = math.max(0, punchScale.Value - 0.03f),
                    Damage = damage,
                });
            }
            else
            {
                float scaleMult = 1.0f + math.log10(totalDamage) / 8.0f;
                ECB.AddComponent(sortKey, entity, new PunchScaleComponent
                {
                    Duration = 0.2f,
                    StartScale = transform.Scale,
                    PunchScale = transform.Scale * scaleMult,
                    Damage = totalDamage,
                });
            }
        }
    }
}
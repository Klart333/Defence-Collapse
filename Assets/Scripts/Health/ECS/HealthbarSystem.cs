using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Effects.ECS;
using Enemy.ECS;

namespace Health.ECS
{
    [BurstCompile, UpdateAfter(typeof(HealthSystem))]
    public partial struct HealthbarSystem : ISystem
    {
        private ComponentLookup<HealthPropertyComponent> healthLookup;
        private ComponentLookup<ArmorPropertyComponent> armorLookup;
        private ComponentLookup<ShieldPropertyComponent> shieldLookup;
        private BufferLookup<DamageTakenBuffer> damageTakenBufferLookup;
        
        private EntityQuery damageTakenQuery;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            healthLookup = state.GetComponentLookup<HealthPropertyComponent>();
            armorLookup = state.GetComponentLookup<ArmorPropertyComponent>();
            shieldLookup = state.GetComponentLookup<ShieldPropertyComponent>();

            damageTakenBufferLookup = SystemAPI.GetBufferLookup<DamageTakenBuffer>();

            state.RequireForUpdate<DamageTakenTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            healthLookup.Update(ref state);
            armorLookup.Update(ref state);
            shieldLookup.Update(ref state);
            damageTakenBufferLookup.Update(ref state);

            new UpdateBarsJob
            {
                DamageTakenBufferLookup = damageTakenBufferLookup,
                HealthLookup = healthLookup,
                ShieldLookup =  shieldLookup,
                ArmorLookup = armorLookup
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            
        }
    }
    
    [BurstCompile, WithAll(typeof(DamageTakenTag))]
    public partial struct UpdateBarsJob : IJobEntity
    {
        [WriteOnly, NativeDisableParallelForRestriction]
        public ComponentLookup<HealthPropertyComponent> HealthLookup;
        
        [WriteOnly, NativeDisableParallelForRestriction]
        public ComponentLookup<ArmorPropertyComponent> ArmorLookup;
        
        [WriteOnly, NativeDisableParallelForRestriction]
        public ComponentLookup<ShieldPropertyComponent> ShieldLookup;
        
        [ReadOnly]
        public BufferLookup<DamageTakenBuffer> DamageTakenBufferLookup;

        [BurstCompile]
        public void Execute(Entity entity, in Effects.ECS.HealthComponent health, in MaxHealthComponent maxHealth)
        {
            Entity childEntity = health.Bar;
            DynamicBuffer<DamageTakenBuffer> damageTakenBuffer = DamageTakenBufferLookup[entity];
            float totalHealth = maxHealth.Health + maxHealth.Armor + maxHealth.Shield;
            HealthType updatedHealthTypes = 0;

            for (int i = 0; i < damageTakenBuffer.Length; i++)
            {
                switch (damageTakenBuffer[i].DamageTakenType)
                {
                    case HealthType.Health when (updatedHealthTypes & HealthType.Health) == 0:
                        HealthLookup.GetRefRW(childEntity).ValueRW.Value = health.Health / totalHealth;
                        updatedHealthTypes |= HealthType.Health;
                        break;
                    case HealthType.Armor when (updatedHealthTypes & HealthType.Armor) == 0:
                        ArmorLookup.GetRefRW(childEntity).ValueRW.Value = health.Armor / totalHealth;
                        updatedHealthTypes |= HealthType.Armor;
                        break;
                    case HealthType.Shield when (updatedHealthTypes & HealthType.Shield) == 0:
                        ShieldLookup.GetRefRW(childEntity).ValueRW.Value = health.Shield / totalHealth;
                        updatedHealthTypes |= HealthType.Shield;
                        break;
                }   
            }
        }
    }
}
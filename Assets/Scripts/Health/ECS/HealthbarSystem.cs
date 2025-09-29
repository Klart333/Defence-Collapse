using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Effects.ECS;

namespace Health.ECS
{
    [BurstCompile, UpdateAfter(typeof(HealthSystem))]
    public partial struct HealthbarSystem : ISystem
    {
        private ComponentLookup<HealthPropertyComponent> healthLookup;
        private ComponentLookup<ArmorPropertyComponent> armorLookup;
        private ComponentLookup<ShieldPropertyComponent> shieldLookup;
        private EntityQuery damageTakenQuery;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            healthLookup = state.GetComponentLookup<HealthPropertyComponent>();
            armorLookup = state.GetComponentLookup<ArmorPropertyComponent>();
            shieldLookup = state.GetComponentLookup<ShieldPropertyComponent>();

            damageTakenQuery = SystemAPI.QueryBuilder().WithAll<DamageTakenComponent>().Build();
            state.RequireForUpdate(damageTakenQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            healthLookup.Update(ref state);
            armorLookup.Update(ref state);
            shieldLookup.Update(ref state);

            new UpdateBarsJob
            {
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
    
    [BurstCompile]
    public partial struct UpdateBarsJob : IJobEntity
    {
        [WriteOnly, NativeDisableParallelForRestriction]
        public ComponentLookup<HealthPropertyComponent> HealthLookup;
        
        [WriteOnly, NativeDisableParallelForRestriction]
        public ComponentLookup<ArmorPropertyComponent> ArmorLookup;
        
        [WriteOnly, NativeDisableParallelForRestriction]
        public ComponentLookup<ShieldPropertyComponent> ShieldLookup;

        [BurstCompile]
        public void Execute(in Effects.ECS.HealthComponent health, in MaxHealthComponent maxHealth, in DamageTakenComponent damageTaken)
        {
            Entity childEntity = health.Bar;

            float totalHealth = maxHealth.Health + maxHealth.Armor + maxHealth.Shield;
            switch (damageTaken.DamageTakenType)
            {
                case HealthType.Health:
                    HealthLookup.GetRefRW(childEntity).ValueRW.Value = health.Health / totalHealth;
                    break;
                case HealthType.Armor:
                    ArmorLookup.GetRefRW(childEntity).ValueRW.Value = health.Armor / totalHealth;
                    break;
                case HealthType.Shield:
                    ShieldLookup.GetRefRW(childEntity).ValueRW.Value = health.Shield / totalHealth;
                    break;
            }
        }
    }
}
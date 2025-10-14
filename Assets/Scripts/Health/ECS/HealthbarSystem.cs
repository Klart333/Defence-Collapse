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
        
        private EntityQuery damageTakenQuery;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            healthLookup = state.GetComponentLookup<HealthPropertyComponent>();
            armorLookup = state.GetComponentLookup<ArmorPropertyComponent>();

            state.RequireForUpdate<DamageTakenTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            healthLookup.Update(ref state);
            armorLookup.Update(ref state);

            new UpdateBarsJob
            {
                HealthLookup = healthLookup,
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

        [BurstCompile]
        public void Execute(in Effects.ECS.HealthComponent health, in MaxHealthComponent maxHealth)
        {
            Entity childEntity = health.Bar;
            float totalHealth = maxHealth.Health + maxHealth.Armor;
            HealthLookup.GetRefRW(childEntity).ValueRW.Value = health.Health / totalHealth;
            ArmorLookup.GetRefRW(childEntity).ValueRW.Value = health.Armor / totalHealth;
        }
    }
}
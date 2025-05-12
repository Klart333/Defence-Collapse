using Unity.Collections;
using Unity.Transforms;
using Unity.Entities;
using Unity.Burst;
using Effects.ECS;
using UnityEngine;

namespace Health.ECS
{
    [BurstCompile, UpdateAfter(typeof(HealthSystem))]
    public partial struct HealthbarSystem : ISystem
    {
        private BufferLookup<Child> childLookup;
        private ComponentLookup<HealthPropertyComponent> healthLookup;
        private ComponentLookup<ArmorPropertyComponent> armorLookup;
        private ComponentLookup<ShieldPropertyComponent> shieldLookup;
        private EntityQuery damageTakenQuery;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            childLookup = state.GetBufferLookup<Child>(true);
            healthLookup = state.GetComponentLookup<HealthPropertyComponent>();
            armorLookup = state.GetComponentLookup<ArmorPropertyComponent>();
            shieldLookup = state.GetComponentLookup<ShieldPropertyComponent>();

            EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp).WithAll<DamageTakenComponent>();
            damageTakenQuery = state.GetEntityQuery(builder);
            
            builder.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (damageTakenQuery.IsEmpty)
            {
                return;
            }
            
            childLookup.Update(ref state);
            healthLookup.Update(ref state);
            armorLookup.Update(ref state);
            shieldLookup.Update(ref state);

            state.Dependency = new UpdateBarsJob
            {
                HealthLookup = healthLookup,
                ChildLookup = childLookup,
                ShieldLookup =  shieldLookup,
                ArmorLookup = armorLookup
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            
        }
    }
    
    [BurstCompile]
    public partial struct UpdateBarsJob : IJobEntity
    {
        [WriteOnly]
        public ComponentLookup<HealthPropertyComponent> HealthLookup;
        
        [WriteOnly]
        public ComponentLookup<ArmorPropertyComponent> ArmorLookup;
        
        [WriteOnly]
        public ComponentLookup<ShieldPropertyComponent> ShieldLookup;
        
        [ReadOnly]
        public BufferLookup<Child> ChildLookup;

        [BurstCompile]
        public void Execute(Entity entity, in Effects.ECS.HealthComponent health, in MaxHealthComponent maxHealth, in DamageTakenComponent damageTaken)
        {
            Entity childEntity = ChildLookup[entity][0].Value;

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
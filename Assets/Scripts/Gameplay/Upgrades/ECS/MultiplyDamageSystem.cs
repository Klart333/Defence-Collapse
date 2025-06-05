using Effects.ECS;
using Health;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Gameplay.Upgrades.ECS
{
    [BurstCompile, UpdateBefore(typeof(AddComponentsSystem))]
    public partial struct MultiplyDamageSystem : ISystem
    {
        private EntityQuery damageComponentQuery;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            EntityQueryBuilder builder2 = new EntityQueryBuilder(state.WorldUpdateAllocator).WithAll<MultiplyDamageComponent>();
            damageComponentQuery = state.GetEntityQuery(builder2);
            state.RequireForUpdate(damageComponentQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            NativeArray<MultiplyDamageComponent> components = damageComponentQuery.ToComponentDataArray<MultiplyDamageComponent>(Allocator.TempJob);

            state.Dependency = new MultiplyDamageJob
            {
                DamageComponents = components,
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }

    [BurstCompile]
    public partial struct MultiplyDamageJob : IJobEntity
    {
        [ReadOnly, DeallocateOnJobCompletion]
        public NativeArray<MultiplyDamageComponent> DamageComponents;
        
        public void Execute(ref DamageComponent damage, in AddComponentInitComponent init)
        {
            foreach (MultiplyDamageComponent multiplyDamageComponent in DamageComponents)
            {
                if ((init.CategoryType & multiplyDamageComponent.AppliedCategory) == 0)
                {
                    continue;
                }

                if ((multiplyDamageComponent.AppliedHealthType & HealthType.Health) != 0)
                {
                    damage.HealthDamage *= multiplyDamageComponent.DamageMultiplier;
                }
                
                if ((multiplyDamageComponent.AppliedHealthType & HealthType.Armor) != 0)
                {
                    damage.ArmorDamage *= multiplyDamageComponent.DamageMultiplier;
                }
                
                if ((multiplyDamageComponent.AppliedHealthType & HealthType.Shield) != 0)
                {
                    damage.ShieldDamage *= multiplyDamageComponent.DamageMultiplier;
                }
            }
        }
    }

    public struct MultiplyDamageComponent : IComponentData
    {
        public CategoryType AppliedCategory;
        public Health.HealthType AppliedHealthType;
        public float DamageMultiplier;
    }
}
using Unity.Collections;
using Unity.Entities;
using Effects.ECS;
using Unity.Burst;

namespace Gameplay.Upgrades.ECS
{
    [BurstCompile, UpdateAfter(typeof(DeathSystem)), UpdateBefore(typeof(AddComponentsSystem))]
    public partial struct MultiplyDamageSystem : ISystem
    {
        private EntityQuery damageComponentQuery;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            damageComponentQuery = SystemAPI.QueryBuilder().WithAll<MultiplyDamageComponent>().Build();
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

                damage.Damage *= multiplyDamageComponent.DamageMultiplier;
            }
        }
    }

    public struct MultiplyDamageComponent : IComponentData
    {
        public CategoryType AppliedCategory;
        public float DamageMultiplier;
    }
}
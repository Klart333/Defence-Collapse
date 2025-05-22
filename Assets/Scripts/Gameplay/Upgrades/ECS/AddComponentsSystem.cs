using Unity.Collections;
using Unity.Entities;
using Effects.ECS;
using Unity.Burst;

namespace Gameplay.Upgrades.ECS
{
    public partial struct AddComponentsSystem : ISystem 
    {
        private EntityQuery componentQuery;  
            
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            EntityQueryBuilder builder = new EntityQueryBuilder(state.WorldUpdateAllocator).WithAll<AddComponentInitComponent>();
            state.RequireForUpdate(state.GetEntityQuery(builder));
            
            EntityQueryBuilder builder2 = new EntityQueryBuilder(state.WorldUpdateAllocator).WithAll<AddComponentComponent>();
            componentQuery = state.GetEntityQuery(builder2);
            state.RequireForUpdate(componentQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            NativeArray<AddComponentComponent> components = componentQuery.ToComponentDataArray<AddComponentComponent>(Allocator.TempJob);
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
            state.Dependency = new AddComponentJob
            {
                AddComponents = components,
                ECB = ecb,
            }.Schedule(state.Dependency);
            
            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            components.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) 
        {

        }
    }

    public partial struct AddComponentJob : IJobEntity
    {
        [ReadOnly]
        public NativeArray<AddComponentComponent> AddComponents;

        public EntityCommandBuffer ECB;
        
        public void Execute(Entity entity, AddComponentInitComponent addComponentInit)
        {
            ECB.RemoveComponent<AddComponentInitComponent>(entity);
            
            foreach (AddComponentComponent component in AddComponents)
            {
                if ((component.AppliedCategory & addComponentInit.CategoryType) == 0) continue;

                switch (component.ComponentType)
                {
                    case UpgradeComponentType.Fire: ECB.AddComponent(entity, new FireComponent{TotalDamage = component.Strength}); break;
                    case UpgradeComponentType.Lightning: ECB.AddComponent(entity, new LightningComponent {Bounces = (int)component.Strength}); break;
                    case UpgradeComponentType.Explosion: ECB.AddComponent(entity, new ExplosionComponent()); break;
                    case UpgradeComponentType.MoneyOnDeath: ECB.AddComponent(entity, new MoneyOnDeathComponent {Amount = component.Strength}); break;
                };
                
            }
        }
    } 

    public struct AddComponentInitComponent : IComponentData
    {
        public CategoryType CategoryType;
    }
}
using Unity.Collections;
using Effects.ECS.ECB;
using Unity.Entities;
using Effects.ECS;
using Unity.Burst;

namespace Gameplay.Upgrades.ECS
{
    [BurstCompile, UpdateAfter(typeof(DeathSystem)), UpdateBefore(typeof(CollisionSystem))] 
    public partial struct AddComponentsSystem : ISystem 
    {
        private EntityQuery componentQuery;
            
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            componentQuery = SystemAPI.QueryBuilder().WithAll<AddComponentComponent>().Build();
            
            state.RequireForUpdate<BeforeCollisionECBSystem.Singleton>();
            state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<AddComponentInitComponent>().Build());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<BeforeCollisionECBSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            if (componentQuery.IsEmpty)
            {
                new RemoveTagJob
                {
                    ECB = ecb.AsParallelWriter(),
                }.ScheduleParallel();
                return;
            }
            
            NativeArray<AddComponentComponent> components = componentQuery.ToComponentDataArray<AddComponentComponent>(Allocator.TempJob);
            new AddComponentJob
            {
                AddComponents = components,
                ECB = ecb.AsParallelWriter(),
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) 
        {

        }
    }

    [BurstCompile]
    public partial struct AddComponentJob : IJobEntity
    {
        [ReadOnly, DeallocateOnJobCompletion]
        public NativeArray<AddComponentComponent> AddComponents;

        public EntityCommandBuffer.ParallelWriter ECB;
        
        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, AddComponentInitComponent addComponentInit)
        {
            ECB.RemoveComponent<AddComponentInitComponent>(sortKey, entity);
            
            foreach (AddComponentComponent component in AddComponents)
            {
                if ((component.AppliedCategory & addComponentInit.CategoryType) == 0) continue;

                switch (component.ComponentType)
                {
                    case UpgradeComponentType.MoneyOnDeath: ECB.AddComponent(sortKey, entity, new MoneyOnDeathComponent { Amount = component.Strength }); break;
                    case UpgradeComponentType.Lightning: ECB.AddComponent(sortKey, entity, new LightningComponent { Bounces = (int)component.Strength, Damage = component.Strength * 5}); break;
                    case UpgradeComponentType.Poison: ECB.AddComponent(sortKey, entity, new PoisonComponent { TotalDamage = component.Strength }); break;
                    case UpgradeComponentType.Fire: ECB.AddComponent(sortKey, entity, new FireComponent { TotalDamage = component.Strength }); break;
                    case UpgradeComponentType.Explosion: ECB.AddComponent(sortKey, entity, new ExplosionComponent()); break;
                };
                
            }
        }
    } 
    
    [BurstCompile, WithAll(typeof(AddComponentInitComponent))]
    public partial struct RemoveTagJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        
        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity)
        {
            ECB.RemoveComponent<AddComponentInitComponent>(sortKey, entity);
        }
    } 

    public struct AddComponentInitComponent : IComponentData
    {
        public CategoryType CategoryType;
    }
}
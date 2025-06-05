using Unity.Collections;
using Unity.Entities;
using Effects.ECS;
using Unity.Burst;

namespace Gameplay.Upgrades.ECS
{
    [UpdateBefore(typeof(CollisionSystem)), BurstCompile]
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
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
            if (componentQuery.IsEmpty)
            {
                state.Dependency = new RemoveTagJob
                {
                    ECB = ecb.AsParallelWriter(),
                }.ScheduleParallel(state.Dependency);
                
                state.Dependency.Complete();
                ecb.Playback(state.EntityManager);
                ecb.Dispose();
                return;
            }
            
            NativeArray<AddComponentComponent> components = componentQuery.ToComponentDataArray<AddComponentComponent>(Allocator.TempJob);
            state.Dependency = new AddComponentJob
            {
                AddComponents = components,
                ECB = ecb.AsParallelWriter(),
            }.ScheduleParallel(state.Dependency);
            
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

    [BurstCompile]
    public partial struct AddComponentJob : IJobEntity
    {
        [ReadOnly]
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
                    case UpgradeComponentType.Lightning: ECB.AddComponent(sortKey, entity, new LightningComponent { Bounces = (int)component.Strength }); break;
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
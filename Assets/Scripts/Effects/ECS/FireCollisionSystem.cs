using Unity.Collections;
using Effects.ECS.ECB;
using Unity.Entities;
using Unity.Burst;

namespace Effects.ECS
{
    [UpdateAfter(typeof(CollisionSystem)), UpdateBefore(typeof(HealthSystem))]
    public partial struct FireCollisionSystem : ISystem
    {
        private ComponentLookup<FireComponent> fireComponentLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PendingDamageECBSystem.Singleton>();
            fireComponentLookup = SystemAPI.GetComponentLookup<FireComponent>(true);
            
            EntityQueryBuilder builder = new EntityQueryBuilder(state.WorldUpdateAllocator).WithAll<FireComponent>();
            state.RequireForUpdate(state.GetEntityQuery(builder));
            
            EntityQueryBuilder builder2 = new EntityQueryBuilder(state.WorldUpdateAllocator).WithAll<PendingDamageComponent>();
            state.RequireForUpdate(state.GetEntityQuery(builder2));
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            fireComponentLookup.Update(ref state);
            var singleton = SystemAPI.GetSingleton<PendingDamageECBSystem.Singleton>();
            EntityCommandBuffer ecb = singleton.CreateCommandBuffer(state.WorldUnmanaged);

            state.Dependency = new FireCollisionJob
            {
                ECB = ecb.AsParallelWriter(),
                FireLookup = fireComponentLookup,
            }.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }

    [BurstCompile]
    public partial struct FireCollisionJob : IJobEntity
    {
        [ReadOnly]
        public ComponentLookup<FireComponent> FireLookup;

        public EntityCommandBuffer.ParallelWriter ECB;
        
        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in PendingDamageComponent pendingDamage)
        {
            if (!FireLookup.TryGetComponent(pendingDamage.SourceEntity, out FireComponent sourceFire)) return;
            
            if (!FireLookup.TryGetComponent(entity, out FireComponent fire))
            {
                fire = new FireComponent();
            }
            
            fire.TotalDamage += sourceFire.TotalDamage;
            ECB.AddComponent(sortKey, entity, fire);
        }
    }
}
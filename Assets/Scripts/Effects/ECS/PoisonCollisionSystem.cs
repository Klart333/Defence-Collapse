using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Effects.ECS
{
    [UpdateAfter(typeof(CollisionSystem)), UpdateBefore(typeof(HealthSystem))]
    public partial struct PoisonCollisionSystem : ISystem
    {
        private ComponentLookup<PoisonComponent> poisonComponentLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PendingDamageECBSystem.Singleton>();
            poisonComponentLookup = SystemAPI.GetComponentLookup<PoisonComponent>(true);
            
            EntityQueryBuilder builder = new EntityQueryBuilder(state.WorldUpdateAllocator).WithAll<PoisonComponent>();
            state.RequireForUpdate(state.GetEntityQuery(builder));
            
            EntityQueryBuilder builder2 = new EntityQueryBuilder(state.WorldUpdateAllocator).WithAll<PendingDamageComponent>();
            state.RequireForUpdate(state.GetEntityQuery(builder2));
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            poisonComponentLookup.Update(ref state);
            var singleton = SystemAPI.GetSingleton<PendingDamageECBSystem.Singleton>();
            EntityCommandBuffer ecb = singleton.CreateCommandBuffer(state.WorldUnmanaged);

            state.Dependency = new PoisonCollisionJob
            {
                ECB = ecb.AsParallelWriter(),
                PoisonLookup = poisonComponentLookup,
            }.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
    
    [BurstCompile]
    public partial struct PoisonCollisionJob : IJobEntity
    {
        [ReadOnly]
        public ComponentLookup<PoisonComponent> PoisonLookup;

        public EntityCommandBuffer.ParallelWriter ECB;
        
        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in PendingDamageComponent pendingDamage)
        {
            if (!PoisonLookup.TryGetComponent(pendingDamage.SourceEntity, out PoisonComponent sourcePoison)) return;
            
            if (!PoisonLookup.TryGetComponent(entity, out PoisonComponent fire))
            {
                fire = new PoisonComponent();                
            }
            
            fire.TotalDamage += sourcePoison.TotalDamage;
            ECB.AddComponent(sortKey, entity, sourcePoison);
        }
    }
}
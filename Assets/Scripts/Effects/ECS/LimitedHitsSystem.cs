using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Effects.ECS
{
    [BurstCompile, UpdateAfter(typeof(DeathSystem))]
    public partial struct LimitedHitsSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob); 
            state.Dependency = new LimitedHitsJob
            {
                ECB = ecb.AsParallelWriter()
            }.ScheduleParallel(state.Dependency);
            
            state.Dependency.Complete(); 
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }

    [BurstCompile, WithNone(typeof(ReloadHitsComponent))]
    public partial struct LimitedHitsJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        
        [BurstCompile]
        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in DamageComponent damageComponent)
        {
            if (damageComponent is { HasLimitedHits: true, LimitedHits: 0 })
            {
                ECB.AddComponent<DeathTag>(sortKey, entity);
            }
        }
    }
}
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
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            new LimitedHitsJob
            {
                ECB = ecb.AsParallelWriter()
            }.ScheduleParallel();
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
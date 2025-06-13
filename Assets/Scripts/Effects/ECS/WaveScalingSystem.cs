using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Effects.ECS
{
    public partial struct WaveScalingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WaveCountComponent>();
            state.RequireForUpdate<HealthScalingComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            int waveCount = SystemAPI.GetSingleton<WaveCountComponent>().Value;
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            state.Dependency = new WaveScalingJob
            {
                WaveCount = waveCount,
                ECB = ecb.AsParallelWriter()
            }.Schedule(state.Dependency);
            
            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
    
    [BurstCompile]
    public partial struct WaveScalingJob : IJobEntity
    {
        public int WaveCount;

        public EntityCommandBuffer.ParallelWriter ECB;

        [BurstCompile]
        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in HealthScalingComponent scalingComponent, ref HealthComponent health, ref MaxHealthComponent maxHealth)
        {
            float multiplier = scalingComponent.Multiplier * 0.02f * WaveCount * WaveCount + -0.04f * WaveCount + 1; // 0.02x^2 + -0.04x + 1
            health.Health *= multiplier;
            health.Armor *= multiplier;
            health.Shield *= multiplier;
            
            maxHealth.Health *= multiplier;
            maxHealth.Armor *= multiplier;
            maxHealth.Shield *= multiplier;
            
            ECB.RemoveComponent<HealthScalingComponent>(sortKey, entity);
        }
    }

    public struct WaveCountComponent : IComponentData
    {
        public int Value;
    }
}
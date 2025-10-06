using Unity.Entities;
using Unity.Burst;
using Enemy.ECS;

namespace Effects.ECS
{
    [BurstCompile, UpdateAfter(typeof(SpawnerSystem))]
    public partial struct WaveScalingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<HealthScalingComponent>();
            state.RequireForUpdate<TurnCountComponent>(); 
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            int turnCount = SystemAPI.GetSingleton<TurnCountComponent>().Value;
            
            new WaveScalingJob
            {
                TurnCount = turnCount,
                ECB = ecb.AsParallelWriter()
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
    
    [BurstCompile]
    public partial struct WaveScalingJob : IJobEntity
    {
        public int TurnCount;

        public EntityCommandBuffer.ParallelWriter ECB;

        [BurstCompile]
        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in HealthScalingComponent scalingComponent, ref HealthComponent health, ref MaxHealthComponent maxHealth)
        {
            float multiplier = scalingComponent.Multiplier * 0.02f * TurnCount * TurnCount + -0.04f * TurnCount + 1; // 0.02x^2 + -0.04x + 1
            health.Health *= multiplier;
            health.Armor *= multiplier;
            health.Shield *= multiplier;
            
            maxHealth.Health *= multiplier;
            maxHealth.Armor *= multiplier;
            maxHealth.Shield *= multiplier;
            
            ECB.RemoveComponent<HealthScalingComponent>(sortKey, entity);
        }
    }

    public struct TurnCountComponent : IComponentData
    {
        public int Value;
    }
}
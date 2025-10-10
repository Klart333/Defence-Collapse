using Unity.Entities;
using Unity.Burst;
using Enemy.ECS;
using Unity.Mathematics;

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
            float turnValue = TurnCount / 25.0f;
            float multiplier = math.pow(3, turnValue); // 3 ^ (x / 25), quite slow
            multiplier = math.max(1.0f, multiplier * scalingComponent.Multiplier);
            
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
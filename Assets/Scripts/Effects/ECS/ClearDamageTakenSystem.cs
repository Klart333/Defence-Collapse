using Enemy.ECS;
using Unity.Burst;
using Unity.Entities;

namespace Effects.ECS
{
    [BurstCompile, UpdateAfter(typeof(HealthSystem))]
    public partial struct ClearDamageTakenSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<DamageTakenTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            new ClearDamageTakenJob
            {
                ECB = ecb.AsParallelWriter(),
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }

    [BurstCompile, WithAll(typeof(DamageTakenTag))]
    public partial struct ClearDamageTakenJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        
        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity)
        {
            ECB.SetBuffer<DamageTakenBuffer>(sortKey, entity);
            ECB.RemoveComponent<DamageTakenTag>(sortKey, entity);
        }
    }
}
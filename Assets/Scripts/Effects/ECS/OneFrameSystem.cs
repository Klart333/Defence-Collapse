using Unity.Burst;
using Unity.Entities;

namespace Effects.ECS
{
    [BurstCompile, UpdateAfter(typeof(DeathSystem))]
    public partial struct OneFrameSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<OneFrameTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            new OneFrameJob
            {
                ECB = ecb.AsParallelWriter(),
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            
        }
    }

    [BurstCompile, WithAll(typeof(OneFrameTag))]
    public partial struct OneFrameJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        
        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity)
        {
            ECB.AddComponent<DeathTag>(sortKey, entity);
        }
    } 
    
    public struct OneFrameTag : IComponentData { }
}
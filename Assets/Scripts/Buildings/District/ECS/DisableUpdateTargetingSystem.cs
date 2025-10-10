using Effects.ECS.ECB;
using Unity.Burst;
using Unity.Entities;

namespace Buildings.District.ECS
{
    [BurstCompile, UpdateAfter(typeof(DistrictTargetingFinalECBSystem))]
    public partial struct DisableUpdateTargetingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<UpdateTargetingTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            new RemoveUpdateTargetingTagJob
            {
                ECB = ecb.AsParallelWriter(),
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
    
    
    [BurstCompile, WithAll(typeof(UpdateTargetingTag))]
    public partial struct RemoveUpdateTargetingTagJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        
        public void Execute([ChunkIndexInQuery]int sortKey, Entity entity)
        {
            ECB.RemoveComponent<UpdateTargetingTag>(sortKey, entity);
        }
    }
}
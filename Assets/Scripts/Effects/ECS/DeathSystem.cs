using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Effects.ECS
{
    public partial struct DeathSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            new DeathJob
            {
                ECB = ecb.AsParallelWriter(),
            }.ScheduleParallel();
            
            state.Dependency.Complete(); 
            ecb.Playback(state.EntityManager);
            ecb.Dispose();   
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }

    [WithAll(typeof(DeathTag))]
    public partial struct DeathJob : IJobEntity
    {
        [ReadOnly]
        public EntityCommandBuffer.ParallelWriter ECB;
        
        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity)
        {
            ECB.DestroyEntity(sortKey, entity);
        }
    }
}
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Effects.ECS
{
    public partial struct DeathSystem : ISystem
    {
        private EntityQuery deathQuery;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            deathQuery = SystemAPI.QueryBuilder().WithAll<DeathTag>().Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (deathQuery.IsEmpty)
            {
                return;
            }
            
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
    [BurstCompile]
    public partial struct DeathJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        
        [BurstCompile]
        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity)
        {
            ECB.DestroyEntity(sortKey, entity);
        }
    }
}
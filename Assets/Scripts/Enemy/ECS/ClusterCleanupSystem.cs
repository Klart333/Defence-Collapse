using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Effects.ECS;

namespace Enemy.ECS
{
    [UpdateBefore(typeof(DeathSystem))] 
    public partial struct ClusterCleanupSystem : ISystem
    {
        private BufferLookup<ManagedEntityBuffer> bufferLookup; 
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            bufferLookup = SystemAPI.GetBufferLookup<ManagedEntityBuffer>();
            
            EntityQuery dyingEnemyQuery = SystemAPI.QueryBuilder().WithAll<ManagedClusterComponent, DeathTag>().Build();
            state.RequireForUpdate(dyingEnemyQuery); 
        }
 
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            bufferLookup.Update(ref state);
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
            
            state.Dependency = new ClustCleanupJob
            {
                BufferLookup = bufferLookup,
                ECB = ecb
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

    [BurstCompile, WithAll(typeof(DeathTag))]
    public partial struct ClustCleanupJob : IJobEntity
    {
        public BufferLookup<ManagedEntityBuffer> BufferLookup;
        public EntityCommandBuffer ECB;
        
        public void Execute(Entity entity, in ManagedClusterComponent cluster)
        {
            DynamicBuffer<ManagedEntityBuffer> buffer = BufferLookup[cluster.ClusterParent];
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].Entity != entity) continue;
                
                buffer.RemoveAtSwapBack(i);
                break;
            }
            
            ECB.AddComponent<UpdatePositioningTag>(cluster.ClusterParent);
        }
    }
}
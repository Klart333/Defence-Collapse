using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Entities;
using Unity.Burst;

namespace Enemy.ECS
{
    public partial struct ClusterPositioningSystem : ISystem 
    {
        private ComponentLookup<LocalTransform> transformLookup;
        private BufferLookup<ManagedEntityBuffer> bufferLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            transformLookup = SystemAPI.GetComponentLookup<LocalTransform>();
            bufferLookup = SystemAPI.GetBufferLookup<ManagedEntityBuffer>();
            
            state.RequireForUpdate<UpdatePositioningTag>();
        }

        [BurstCompile] 
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
            transformLookup.Update(ref state);
            bufferLookup.Update(ref state);
           
            state.Dependency = new ClusterPositioningJob
            {
                BufferLookup = bufferLookup,
                TransformLookup = transformLookup,
                ECB = ecb.AsParallelWriter(),
            }.ScheduleParallel(state.Dependency);
             
            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }

    [BurstCompile, WithAll(typeof(UpdatePositioningTag))]
    public partial struct ClusterPositioningJob : IJobEntity
    {
        [ReadOnly] 
        public BufferLookup<ManagedEntityBuffer> BufferLookup;
        
        [NativeDisableParallelForRestriction]
        public ComponentLookup<LocalTransform> TransformLookup; 

        public EntityCommandBuffer.ParallelWriter ECB;
        
        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in EnemyClusterComponent cluster)
        {
            DynamicBuffer<ManagedEntityBuffer> buffer = BufferLookup[entity];
            
            int bufferLength = buffer.Length;
            int rows = (int)math.floor(math.sqrt(bufferLength));
            int enemiesPerRow = (int)math.ceil((float)bufferLength / rows);
            const float tileSize = 1.75f; // 2 - some padding
            float size = math.min(cluster.EnemySize, tileSize / rows); 
            quaternion rotation = quaternion.LookRotation(new float3(cluster.Facing.x, 0, cluster.Facing.y), new float3(0, 1, 0));
            
            int bufferIndex = 0;
            for (int i = 0; i < rows; i++)
            {
                int enemiesInRow = math.min(enemiesPerRow, bufferLength - bufferIndex);
                for (int j = 0; j < enemiesInRow; j++)
                {
                    RefRW<LocalTransform> transform = TransformLookup.GetRefRW(buffer[bufferIndex++].Entity);
                    float rowOffset = rows / 2.0f * size - i * size;
                    float columnOffset = enemiesInRow / 2.0f * size - j * size;
                    transform.ValueRW.Position = cluster.Position + math.mul(rotation, new float3(columnOffset, 0, rowOffset));
                    transform.ValueRW.Rotation = rotation;
                }
            }

            ECB.RemoveComponent<UpdatePositioningTag>(sortKey, entity);
        }
    }
    
    public struct UpdatePositioningTag : IComponentData { }
}
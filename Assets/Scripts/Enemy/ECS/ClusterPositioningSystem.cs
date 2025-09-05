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
        
        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in EnemyClusterComponent clusterComponent)
        {
            DynamicBuffer<ManagedEntityBuffer> buffer = BufferLookup[entity];
            
            float size = clusterComponent.EnemySize;
            
            int bufferLength = buffer.Length;
            int rows = (int)math.floor(math.sqrt(bufferLength));
            int enemiesPerRow = (int)math.ceil((float)bufferLength / rows);
            
            int bufferIndex = 0;
            for (int i = 0; i < rows; i++)
            {
                int enemiesInRow = math.min(enemiesPerRow, bufferLength - bufferIndex);
                for (int j = 0; j < enemiesInRow; j++)
                {
                    RefRW<LocalTransform> transform = TransformLookup.GetRefRW(buffer[bufferIndex++].Entity);
                    float rowOffset = rows / 2.0f * size - i * size;
                    float columnOffset = enemiesInRow / 2.0f * size - j * size;
                    transform.ValueRW.Position = clusterComponent.Position + new float3(columnOffset, 0, rowOffset);
                }
            }
            
            ECB.RemoveComponent<UpdatePositioningTag>(sortKey, entity);
        }
    }
    
    public struct UpdatePositioningTag : IComponentData { }
}
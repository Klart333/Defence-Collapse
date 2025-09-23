using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Entities;
using Unity.Burst;
using DG.Tweening;
using Effects.ECS;
using Pathfinding;

namespace Enemy.ECS
{
    [BurstCompile]
    public partial struct ClusterPositioningSystem : ISystem 
    {
        private ComponentLookup<LocalTransform> transformLookup;
        private BufferLookup<ManagedEntityBuffer> bufferLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            transformLookup = SystemAPI.GetComponentLookup<LocalTransform>();
            bufferLookup = SystemAPI.GetBufferLookup<ManagedEntityBuffer>();
            
            state.RequireForUpdate<UpdatePositioningComponent>();
        }

        [BurstCompile] 
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            transformLookup.Update(ref state);
            bufferLookup.Update(ref state);
           
            state.Dependency = new ClusterPositioningJob
            {
                BufferLookup = bufferLookup,
                TransformLookup = transformLookup,
                ECB = ecb.AsParallelWriter(),
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }

    [BurstCompile, WithAll(typeof(UpdatePositioningComponent))]
    public partial struct ClusterPositioningJob : IJobEntity
    {
        [ReadOnly] 
        public BufferLookup<ManagedEntityBuffer> BufferLookup;
        
        [ReadOnly]
        public ComponentLookup<LocalTransform> TransformLookup; 

        public EntityCommandBuffer.ParallelWriter ECB;
        
        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in EnemyClusterComponent cluster, ref RandomComponent random)
        {
            DynamicBuffer<ManagedEntityBuffer> buffer = BufferLookup[entity];
            
            const float tileSize = 1.75f; // 2 - some padding
            
            int bufferLength = buffer.Length;
            int rows = (int)math.floor(math.sqrt(bufferLength));
            int enemiesPerRow = (int)math.ceil((float)bufferLength / rows);
            float size = math.min(cluster.EnemySize, tileSize / rows); 
            quaternion targetRotation = quaternion.LookRotation(new float3(cluster.Facing.x, 0, cluster.Facing.y), new float3(0, 1, 0));
            
            int bufferIndex = 0;
            for (int i = 0; i < rows; i++)
            {
                int enemiesInRow = math.min(enemiesPerRow, bufferLength - bufferIndex);
                for (int j = 0; j < enemiesInRow; j++)
                {
                    Entity enemyEntity = buffer[bufferIndex++].Entity;
                    LocalTransform enemyTransform = TransformLookup[enemyEntity];
                    float rowOffset = rows / 2.0f * size - i * size;
                    float columnOffset = enemiesInRow / 2.0f * size - j * size;
                    float3 targetPosition = cluster.Position + math.mul(targetRotation, new float3(columnOffset, 0, rowOffset));
                    float dist = math.length(targetPosition - enemyTransform.Position); 
                    
                    ECB.AddComponent(sortKey, enemyEntity, new ArchedMovementComponent
                    {
                        StartPosition = enemyTransform.Position,
                        EndPosition = targetPosition,
                        Pivot = math.lerp(enemyTransform.Position, targetPosition, 0.5f) + new float3(0, 0.5f * dist, 0), 
                        Value = random.Random.NextFloat(0.0f, 0.1f),
                        Ease = Ease.InOutSine,
                    });
                    
                    ECB.AddComponent(sortKey, enemyEntity, new TargetRotationComponent
                    {
                        StartRotation = enemyTransform.Rotation,
                        EndRotation = targetRotation,
                    });
                }
            }

            ECB.RemoveComponent<UpdatePositioningComponent>(sortKey, entity);
        }
    }

    public struct UpdatePositioningComponent : IComponentData
    {
        public PathIndex CurrentTile;
        public PathIndex PreviousTile;
    }
}
using Unity.Entities;
using Unity.Burst;
using Gameplay;

namespace Enemy.ECS
{
    [BurstCompile, UpdateBefore(typeof(FlowFieldComponent))]
    public partial struct TimeMovingClusterSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<MovingClusterComponent>();
            state.RequireForUpdate<GameSpeedComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            float gameSpeed = SystemAPI.GetSingleton<GameSpeedComponent>().Speed;
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            new TimeMovingClusterJob
            {
                DeltaTime = deltaTime * gameSpeed,
                ECB = ecb.AsParallelWriter()
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
 
        }
    }

    [BurstCompile]
    public partial struct TimeMovingClusterJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;

        public float DeltaTime;
        
        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, ref MovingClusterComponent movingCluster)
        {
            movingCluster.TimeLeft -= DeltaTime;
            if (movingCluster.TimeLeft <= 0)
            {
                ECB.RemoveComponent<MovingClusterComponent>(sortKey, entity);
            }
        }
    }
}
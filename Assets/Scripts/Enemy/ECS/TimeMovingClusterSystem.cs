using Unity.Collections;
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
            state.RequireForUpdate<GameSpeedComponent>();
            state.RequireForUpdate<MovingClusterComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            float gameSpeed = SystemAPI.GetSingleton<GameSpeedComponent>().Speed;
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

            state.Dependency = new TimeMovingClusterJob
            {
                DeltaTime = deltaTime * gameSpeed,
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

    [BurstCompile]
    public partial struct TimeMovingClusterJob : IJobEntity
    {
        public EntityCommandBuffer ECB;

        public float DeltaTime;
        
        public void Execute(Entity entity, ref MovingClusterComponent movingCluster)
        {
            movingCluster.TimeLeft -= DeltaTime;
            if (movingCluster.TimeLeft <= 0)
            {
                ECB.RemoveComponent<MovingClusterComponent>(entity);
            }
        }
    }
}
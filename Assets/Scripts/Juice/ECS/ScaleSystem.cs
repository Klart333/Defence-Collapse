using Gameplay;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Utility;

namespace Juice.Ecs
{
    public partial struct ScaleSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GameSpeedComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float gameSpeed = SystemAPI.GetSingleton<GameSpeedComponent>().Speed;
            float deltaTime = SystemAPI.Time.DeltaTime;

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            new ScaleJob
            {
                DeltaTime = deltaTime * gameSpeed,
                ECB = ecb.AsParallelWriter(),
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }

    [BurstCompile]
    public partial struct ScaleJob : IJobEntity
    {
        public float DeltaTime;
        
        public EntityCommandBuffer.ParallelWriter ECB;
        
        [BurstCompile]
        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, ref LocalTransform transform, ref ScaleComponent scaleComponent)
        {
            scaleComponent.Value = math.min(1.0f, scaleComponent.Value + DeltaTime / scaleComponent.Duration);
            transform.Scale = math.lerp(scaleComponent.StartScale, scaleComponent.TargetScale, Math.EaseOutElastic(scaleComponent.Value));

            if (scaleComponent.Value >= 1.0f)
            {
                ECB.RemoveComponent<ScaleComponent>(sortKey, entity);
            }
        }
    }

    
    public struct ScaleComponent : IComponentData
    {
        public float TargetScale;
        public float StartScale;
        public float Duration;
        public float Value;
    }
}
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Entities;
using Unity.Burst;
using Gameplay;
using Utility;

namespace Effects.ECS
{
    [BurstCompile, UpdateAfter(typeof(PunchScalerSystem))]
    public partial struct PunchScalingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GameSpeedComponent>();
            state.RequireForUpdate<PunchScaleComponent>();  
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float gameSpeed = SystemAPI.GetSingleton<GameSpeedComponent>().Speed;
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            new PunchScaleJob
            {
                ECB = ecb.AsParallelWriter(),
                DeltaTime = SystemAPI.Time.DeltaTime * gameSpeed,
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }

    [BurstCompile, WithNone(typeof(DeathTag))]
    public partial struct PunchScaleJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        public float DeltaTime;
        
        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, ref PunchScaleComponent punchScaleComponent, ref LocalTransform transform)
        {
            punchScaleComponent.Value = math.min(1.0f, punchScaleComponent.Value + DeltaTime / punchScaleComponent.Duration);
            transform.Scale = math.lerp(punchScaleComponent.StartScale, punchScaleComponent.PunchScale, Math.PunchInOut(punchScaleComponent.Value));

            if (punchScaleComponent.Value >= 1.0f)
            {
                ECB.RemoveComponent<PunchScaleComponent>(sortKey, entity);
            }
        }
    }

    public struct PunchScaleComponent : IComponentData
    {
        public float StartScale;
        public float PunchScale;
        public float Duration;

        public float Damage;
        public float Value;
    }
}
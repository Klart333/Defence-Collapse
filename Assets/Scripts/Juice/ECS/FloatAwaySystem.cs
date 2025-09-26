using Gameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Juice.Ecs
{
    public partial struct FloatAwaySystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GameSpeedComponent>();
            state.RequireForUpdate<FloatAwayComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float gameSpeed = SystemAPI.GetSingleton<GameSpeedComponent>().Speed;
            float deltaTime = SystemAPI.Time.DeltaTime;

            new FloatAwayJob
            {
                DeltaTime = deltaTime * gameSpeed,
            }.ScheduleParallel();

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            new DecreaseDurationJob
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
    public partial struct FloatAwayJob : IJobEntity
    {
        public float DeltaTime;
        
        [BurstCompile]
        public void Execute(ref LocalTransform transform, in FloatAwayComponent floatAway)
        {
            transform.Position += floatAway.Direction * floatAway.Speed * DeltaTime;
        }
    }
    
    
    [BurstCompile]
    public partial struct DecreaseDurationJob : IJobEntity
    {
        public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter ECB;
        
        [BurstCompile]
        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, ref FloatAwayComponent floatAway)
        {
            floatAway.Duration -= DeltaTime;
            if (floatAway.Duration > 0.2f) return;
            
            ECB.RemoveComponent<FloatAwayComponent>(sortKey, entity);
            ECB.RemoveComponent<RotateTowardsCamera>(sortKey, entity);
            ECB.AddComponent(sortKey, entity, new ScaleComponent
            {
                Duration = 0.2f,
                StartScale = 1,
                TargetScale = 0,
            });
            
            ECB.AddComponent(sortKey, entity, new RotationComponent
            {
                StartRotation = quaternion.identity,
                EndRotation = quaternion.AxisAngle(new float3(0, 0, 1), math.PI2),
                Speed = 5,
            });
        }
    }
    
    public struct FloatAwayComponent : IComponentData
    {
        public float Duration;
        public float Speed;
        public float3 Direction;
    }
}
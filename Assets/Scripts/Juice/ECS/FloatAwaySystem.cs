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

            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
            state.Dependency = new DecreaseDurationJob
            {
                DeltaTime = deltaTime * gameSpeed,
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
                Duration = 0.2f,
                TargetRotation = 1f,
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
using DG.Tweening;
using Effects.ECS;
using Enemy.ECS;
using Unity.Transforms;
using Unity.Entities;
using Unity.Burst;
using Gameplay;
using Unity.Mathematics;

namespace Juice.Ecs
{
    [BurstCompile, UpdateAfter(typeof(DeathSystem))]
    public partial struct RotationSystem : ISystem
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

            new RotationJob
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
    public partial struct RotationJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        
        public float DeltaTime;
        
        [BurstCompile]
        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, ref LocalTransform transform, ref RotationComponent rotationComponent)
        {
            rotationComponent.Value = math.min(1.0f, rotationComponent.Value + DeltaTime * rotationComponent.Speed);
            transform.Rotation = math.slerp(rotationComponent.StartRotation, rotationComponent.EndRotation, rotationComponent.Value);

            if (rotationComponent.Value >= 1.0f)
            {
                ECB.RemoveComponent<RotationComponent>(sortKey, entity);
            }
        }
    }
    
    public struct RotationComponent : IComponentData
    {
        public quaternion StartRotation;
        public quaternion EndRotation;
        
        public float Value;
        public float Speed;

        public Ease Ease;
    }
}
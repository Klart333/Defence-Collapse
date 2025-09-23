using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Entities;
using DG.Tweening;
using Unity.Burst;
using Enemy.ECS;
using Gameplay;
using Utility;

namespace Effects.ECS
{
    [BurstCompile, UpdateAfter(typeof(CollisionSystem))]
    public partial struct SmoothMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GameSpeedComponent>();
            state.RequireForUpdate<SmoothMovementComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float gameSpeed = SystemAPI.GetSingleton<GameSpeedComponent>().Speed;
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            state.Dependency = new SmoothMovementJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime * gameSpeed,
                ECB = ecb.AsParallelWriter(),
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
    
    [BurstCompile]
    public partial struct SmoothMovementJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        
        public float DeltaTime;
        
        [BurstCompile]
        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, ref LocalTransform transform, ref SmoothMovementComponent move, in SpeedComponent speed)
        {
            move.Value = math.min(1.0f, move.Value + speed.Speed * DeltaTime);
            
            transform.Position = math.lerp(move.StartPosition, move.EndPosition, move.Ease switch
            {
                Ease.InOutSine => Math.InOutSine(move.Value), 
                _ => move.Value 
            });

            if (move.Value >= 1.0f)
            {
                ECB.RemoveComponent<SmoothMovementComponent>(sortKey, entity);
            }
        }
    }

    public struct SmoothMovementComponent : IComponentData
    {
        public float3 StartPosition;
        public float3 EndPosition;
        public float Value;
        public Ease Ease;
    }
}
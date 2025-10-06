using Unity.Mathematics;
using Unity.Transforms;
using Effects.ECS.ECB;
using Unity.Entities;
using Unity.Burst;
using Enemy.ECS;
using Gameplay;

namespace Effects.ECS
{
    [BurstCompile, UpdateAfter(typeof(DeathSystem)), UpdateBefore(typeof(BeforeCollisionECBSystem))]
    public partial struct SimpleMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameSpeedComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            float gameSpeed = SystemAPI.GetSingleton<GameSpeedComponent>().Speed;
            
            new SimpleMovementJob
            {
                DeltaTime = deltaTime * gameSpeed,
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }

    public partial struct SimpleMovementJob : IJobEntity
    {
        public float DeltaTime;
        
        public void Execute(ref LocalTransform transform, in MovementDirectionComponent moveDir, in SpeedComponent speed)
        {
            transform.Position += moveDir.Direction * speed.Speed * DeltaTime;
        }
    }

    public struct MovementDirectionComponent : IComponentData
    {
        public float3 Direction;
    }
}
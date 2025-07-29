using Enemy.ECS;
using Gameplay;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace Effects.ECS
{
    [UpdateBefore(typeof(CollisionSystem))]
    public partial struct ArchedMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameSpeedComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            float gameSpeed = SystemAPI.GetSingleton<GameSpeedComponent>().Speed;
        
            new ArchedMovementJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime * gameSpeed,
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
    
    [BurstCompile]
    public partial struct ArchedMovementJob : IJobEntity
    {
        public float DeltaTime;
        
        [BurstCompile]
        public void Execute(ref LocalTransform transform, ref ArchedMovementComponent arch, in SpeedComponent speed)
        {
            arch.Value += speed.Speed * DeltaTime;
            
            transform.Position = Utility.Math.CubicLerp(arch.StartPosition, arch.EndPosition, arch.Pivot, arch.Value);
        }
    }
}
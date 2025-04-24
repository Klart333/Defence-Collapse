using Gameplay;
using Unity.Burst;
using Unity.Entities;

namespace Effects.ECS
{
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
        public void Execute(ref PositionComponent position, ref ArchedMovementComponent arch, in SpeedComponent speed)
        {
            arch.Value += speed.Speed * DeltaTime;
            
            position.Position = Math.CubicLerp(arch.StartPosition, arch.EndPosition, arch.Pivot, arch.Value);
        }
    }
}
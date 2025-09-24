using Unity.Mathematics;
using Unity.Transforms;
using Unity.Entities;
using Unity.Burst;
using DG.Tweening;
using Enemy.ECS;
using Gameplay;
using Utility;

namespace Effects.ECS
{
    [BurstCompile, UpdateAfter(typeof(CollisionSystem))]
    public partial struct ArchedMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeforeCollisionECBSystem.Singleton>();
            state.RequireForUpdate<GameSpeedComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float gameSpeed = SystemAPI.GetSingleton<GameSpeedComponent>().Speed;

            var ecbSingleton = SystemAPI.GetSingleton<BeforeCollisionECBSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            state.Dependency = new ArchedMovementJob
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
    public partial struct ArchedMovementJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        
        public float DeltaTime;
        
        [BurstCompile]
        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, ref LocalTransform transform, ref ArchedMovementComponent arch, in SpeedComponent speed)
        {
            arch.Value = math.min(1.0f, arch.Value + speed.Speed * DeltaTime);
            
            transform.Position = Math.CubicLerp(arch.StartPosition, arch.EndPosition, arch.Pivot, arch.Ease switch
            {
                Ease.InOutSine => Math.InOutSine(arch.Value), 
               _ => arch.Value 
            });

            if (arch.Value >= 1.0f)
            {
                ECB.RemoveComponent<ArchedMovementComponent>(sortKey, entity);
            }
        }
    }
}
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Entities;
using Unity.Burst;
using Enemy.ECS;
using Gameplay;

namespace Effects.ECS
{
    [BurstCompile, UpdateBefore(typeof(CollisionSystem))] 
    public partial struct TargetRotationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameSpeedComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float gameSpeed = SystemAPI.GetSingleton<GameSpeedComponent>().Speed;
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
            
            state.Dependency = new TargetRotationJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime * gameSpeed,
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
    public partial struct TargetRotationJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        
        public float DeltaTime;
        
        [BurstCompile]
        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, ref LocalTransform transform, ref TargetRotationComponent rotationComponent, in SpeedComponent speed)
        {
            rotationComponent.Value = math.min(1.0f, rotationComponent.Value + speed.Speed * DeltaTime);
            transform.Rotation = math.slerp(rotationComponent.StartRotation, rotationComponent.EndRotation, rotationComponent.Value);

            if (rotationComponent.Value >= 1.0f)
            {
                ECB.RemoveComponent<TargetRotationComponent>(sortKey, entity);
            }
        }
    }
}
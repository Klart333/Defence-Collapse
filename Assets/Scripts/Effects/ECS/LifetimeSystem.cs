using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Gameplay;

namespace Effects.ECS
{
    [UpdateAfter(typeof(DeathSystem)), UpdateBefore(typeof(ArchedMovementSystem))]
    public partial struct LifetimeSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameSpeedComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            float gameSpeed = SystemAPI.GetSingleton<GameSpeedComponent>().Speed;

            new LifetimeJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime * gameSpeed,
                ECB = ecb.AsParallelWriter(),
            }.ScheduleParallel();
            
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
    public partial struct LifetimeJob : IJobEntity
    {
        [ReadOnly]
        public float DeltaTime;

        public EntityCommandBuffer.ParallelWriter ECB;
        
        [BurstCompile]
        public void Execute([ChunkIndexInQuery]int index, Entity entity, ref LifetimeComponent component)
        {
            component.Lifetime -= DeltaTime;

            if (component.Lifetime <= 0)
            {
                ECB.AddComponent(index, entity, new DeathTag());
            }
        }
    }
}
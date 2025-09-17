using Gameplay.Turns.ECS;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Entities;
using Unity.Burst;

namespace Enemy.ECS
{
    [BurstCompile, UpdateBefore(typeof(FlowMovementSystem))]
    public partial struct UpdateClusterPositionSystem : ISystem
    {
        private EntityQuery updateEnemiesQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            updateEnemiesQuery = SystemAPI.QueryBuilder().WithAll<TurnIncreaseComponent, UpdateEnemiesTag>().Build();
            state.RequireForUpdate(updateEnemiesQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            TurnIncreaseComponent turnIncrease = updateEnemiesQuery.GetSingleton<TurnIncreaseComponent>();
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

            state.Dependency = new UpdateClusterPositionJob
            {
                TurnIncrease = turnIncrease.TurnIncrease,
                ECB = ecb.AsParallelWriter()
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
    public partial struct UpdateClusterPositionJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        
        public int TurnIncrease;
        
        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, ref FlowFieldComponent flowField, in MovementSpeedComponent speed)
        {
            flowField.MoveTimer -= TurnIncrease;
            if (flowField.MoveTimer > 0) return;
            
            int count = math.max(1, (int)math.ceil(-flowField.MoveTimer / speed.Speed));
            flowField.MoveTimer += speed.Speed * count;
            
            ECB.AddComponent(sortKey, entity, new UpdateClusterPositionComponent { Count = count });
        }
    }
}
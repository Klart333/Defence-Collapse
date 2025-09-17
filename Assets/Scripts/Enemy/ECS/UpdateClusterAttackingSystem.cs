using Gameplay.Turns.ECS;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Burst;

namespace Enemy.ECS
{
    [BurstCompile, UpdateBefore(typeof(AttackingSystem))]
    public partial struct UpdateClusterAttackingSystem : ISystem
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

            state.Dependency = new UpdateClusterAttackingJob
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

    [BurstCompile, WithAll(typeof(AttackingComponent))]
    public partial struct UpdateClusterAttackingJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        
        public int TurnIncrease;
        
        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, ref AttackSpeedComponent attackSpeedComponent)
        {
            attackSpeedComponent.AttackTimer -= TurnIncrease;
            if (attackSpeedComponent.AttackTimer > 0) return;
            
            int count = math.max(1, (int)math.ceil(-attackSpeedComponent.AttackTimer / attackSpeedComponent.AttackSpeed));
            attackSpeedComponent.AttackTimer += attackSpeedComponent.AttackSpeed * count;
            
            ECB.AddComponent(sortKey, entity, new UpdateClusterAttackingComponent { Count = count });
        }
    }
}
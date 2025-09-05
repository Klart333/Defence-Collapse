using Effects.LittleDudes;
using Unity.Entities;
using Effects.ECS;
using Unity.Burst;

namespace Enemy.ECS
{
    [UpdateInGroup(typeof(SimulationSystemGroup)), UpdateAfter(typeof(SpawnerSystem)), 
     UpdateBefore(typeof(CollisionSystem)), UpdateBefore(typeof(EnemyHashGridSystem))]
    [BurstCompile]
    public partial struct EnemyCountSystem : ISystem // TODO: Remove this basically
    {
        private EntityQuery enemyQuery;
        
        public void OnCreate(ref SystemState state)
        {
            enemyQuery = SystemAPI.QueryBuilder().WithAll<ManagedClusterComponent>().Build();
            
           state.EntityManager.CreateEntity(typeof(WaveStateComponent));
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            SystemAPI.GetSingletonRW<WaveStateComponent>().ValueRW.EnemyCount = enemyQuery.CalculateEntityCount();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }

    public struct WaveStateComponent : IComponentData
    {
        public int EnemyCount;
    }
}
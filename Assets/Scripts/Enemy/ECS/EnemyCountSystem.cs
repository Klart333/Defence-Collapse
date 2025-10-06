using Unity.Entities;
using Effects.ECS;
using Unity.Burst;

namespace Enemy.ECS
{
    [BurstCompile]
    [UpdateAfter(typeof(SpawnerSystem)), UpdateBefore(typeof(CollisionSystem)), 
     UpdateBefore(typeof(EnemyHashGridSystem))]
    public partial struct EnemyCountSystem : ISystem
    {
        private EntityQuery clusterQuery;
        
        public void OnCreate(ref SystemState state)
        {
            clusterQuery = SystemAPI.QueryBuilder().WithAll<EnemyClusterComponent>().Build();
            
           state.EntityManager.CreateEntity(typeof(WaveStateComponent));
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            SystemAPI.GetSingletonRW<WaveStateComponent>().ValueRW.ClusterCount = clusterQuery.CalculateEntityCount();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }

    public struct WaveStateComponent : IComponentData
    {
        public int ClusterCount;
    }
}
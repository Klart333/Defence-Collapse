using Effects.ECS;
using Unity.Entities;

namespace Enemy.ECS
{
    [UpdateInGroup(typeof(SimulationSystemGroup)), UpdateAfter(typeof(SpawnerSystem)), 
     UpdateBefore(typeof(CollisionSystem)), UpdateBefore(typeof(EnemyHashGridSystem))]
    public partial class EnemyCountSystem : SystemBase
    {
        private EntityQuery enemyQuery;
        private EntityQuery spawnerQuery;
        
        private bool inWave;
    
        protected override void OnCreate()
        {
            enemyQuery = new EntityQueryBuilder(WorldUpdateAllocator)
                .WithAll<FlowFieldComponent>()
                .Build(this);
            
            spawnerQuery = SystemAPI.QueryBuilder()
                .WithAspect<SpawnPointAspect>()
                .Build();
            
            Events.OnWaveStarted += OnWaveStarted;

            EntityManager.CreateEntity(typeof(WaveStateComponent));
        }

        private void OnWaveStarted()
        {
            inWave = true;
        }

        protected override void OnUpdate()
        {
            if (!inWave) return;
            
            SystemAPI.GetSingletonRW<WaveStateComponent>().ValueRW.EnemyCount = enemyQuery.CalculateEntityCount();
            
            if (enemyQuery.IsEmpty && spawnerQuery.IsEmpty)
            {
                inWave = false;
                Events.OnWaveEnded.Invoke();
            }
        }

        protected override void OnDestroy()
        {
            Events.OnWaveStarted -= OnWaveStarted;
        }
    }

    public struct WaveStateComponent : IComponentData
    {
        public int EnemyCount;
    }
}
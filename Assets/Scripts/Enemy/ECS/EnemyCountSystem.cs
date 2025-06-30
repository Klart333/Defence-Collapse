using Effects.ECS;
using Effects.LittleDudes;
using Unity.Entities;
using UnityEngine;

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
            enemyQuery = SystemAPI.QueryBuilder().WithAll<FlowFieldComponent>().WithNone<LittleDudeComponent>().Build();
            spawnerQuery = SystemAPI.QueryBuilder().WithAspect<SpawnPointAspect>().Build();
            
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
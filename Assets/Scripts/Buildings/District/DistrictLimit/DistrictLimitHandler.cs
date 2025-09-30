using System;
using Enemy;
using Enemy.ECS.Boss;
using Gameplay.Event;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;
using WaveFunctionCollapse;

namespace Buildings.District.DistrictLimit
{
    public class DistrictLimitHandler : MonoBehaviour
    {
        public event Action<int> DistrictsBuiltChanged;
        
        [Title("References")]
        [SerializeField]
        private UIDistrictLimitDisplay districtLimitDisplay;

        [SerializeField]
        private EnemySpawnHandler enemySpawnHandler;
        
        [Title("Settings")]
        [SerializeField]
        private int districtLimit = 10;

        private EntityManager entityManager;
        
        private bool districtLimitReached;
        private bool spawnedBossData;
        private int districtsBuilt;
        
        private void OnEnable()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;            
            
            Events.OnDistrictBuilt += OnDistrictBuilt;
            Events.OnBuiltIndexDestroyed += OnBuiltIndexDestroyed;
        }

        private void OnDisable()
        {
            Events.OnDistrictBuilt -= OnDistrictBuilt;
            Events.OnBuiltIndexDestroyed -= OnBuiltIndexDestroyed;
        }

        private void OnDistrictBuilt(TowerData towerData)
        {
            switch (towerData.DistrictType)
            {
                case DistrictType.Lumbermill:
                    return;
                case DistrictType.TownHall:
                    districtLimitDisplay.DisplaySegments(this, districtLimit);
                    return;
            }

            districtsBuilt++;
            DistrictsBuiltChanged?.Invoke(1);
            
            if (districtsBuilt < districtLimit) return;
            
            districtLimitReached = true;
            Events.OnDistrictLimitReached?.Invoke();
            if (!spawnedBossData)
            {
                SpawnBossData();
            }
        }
        
        private void OnBuiltIndexDestroyed(ChunkIndex arg0)
        {
            districtsBuilt--;
            DistrictsBuiltChanged?.Invoke(-1);

            if (districtLimitReached)
            {
                districtLimitReached = false;
                Events.OnDistrictLimitUnReached?.Invoke();
            }
            
            if (districtsBuilt < 0)
            {
                Debug.LogWarning("District limit should never be negative: " + districtsBuilt);
            }
        }

        private void SpawnBossData()
        {
            if (spawnedBossData) return;
            
            spawnedBossData = true;

            int spawnIndex = enemySpawnHandler.GetFurthestSpawnIndex();
            Entity entity = entityManager.CreateEntity((typeof(SpawnBossComponent)));
            entityManager.SetComponentData(entity, new SpawnBossComponent
            {
                BossIndex = 100,
                SpawnPointIndex = spawnIndex,
            });
        }
    }
}
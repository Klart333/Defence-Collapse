using System;
using Enemy;
using Enemy.ECS.Boss;
using Gameplay.Event;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

namespace Buildings.District.DistrictLimit
{
    public class DistrictLimitHandler : MonoBehaviour
    {
        [Title("References")]
        [SerializeField]
        private UIDistrictLimitDisplay districtLimitDisplay;

        [SerializeField]
        private EnemySpawnHandler enemySpawnHandler;
        
        [Title("Settings")]
        [SerializeField]
        private int districtLimit = 10;

        private EntityManager entityManager;
        
        private bool spawnedBossData;
        private int districtsBuilt;
        
        private void OnEnable()
        {
            Events.OnDistrictBuilt += OnDistrictBuilt;
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;            
        }

        private void OnDisable()
        {
            Events.OnDistrictBuilt -= OnDistrictBuilt;
        }

        private void OnDistrictBuilt(TowerData towerData)
        {
            if (towerData.DistrictType == DistrictType.TownHall)
            {
                districtLimitDisplay.DisplaySegments(districtLimit);
                return;
            }

            districtsBuilt++;
            if (districtsBuilt < districtLimit) return;
            
            Debug.Log("District limit reached");
            Events.OnDistrictLimitReached?.Invoke();
            if (!spawnedBossData)
            {
                SpawnBossData();
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
                IsFinal = true,
            });
        }
    }
}
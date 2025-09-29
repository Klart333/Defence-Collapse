using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Buildings.District;
using Gameplay.Event;
using UnityEngine;
using System;
using Enemy.ECS.Boss;
using Unity.Entities;

namespace Gameplay
{
    public class GameStateHandler : MonoBehaviour
    {
        [Title("Game over")]
        [SerializeField]
        private GameObject gameOverPanelPrefab;

        [SerializeField]
        private float gameOverDelay = 2;
        
        [Title("Victory")]
        [SerializeField]
        private GameObject victoryPanelPrefab;
        
        [SerializeField]
        private float victoryDelay = 2;
        
        private EntityManager entityManager;
        private EntityQuery winOnDeathQuery;
        
        private bool winOnDeathSpawned = false;
        private bool checkForWinOnDeath = false;
        
        private void OnEnable()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            winOnDeathQuery = entityManager.CreateEntityQuery(typeof(WinOnDeathTag));
            
            Events.OnCapitolDestroyed += OnCapitolDestroyed;
            Events.OnFinalBossDeafeted += OnFinalBossDeafeted;
            Events.OnDistrictLimitReached += OnDistrictLimitReached;
        }

        private void OnDisable()
        {
            Events.OnCapitolDestroyed -= OnCapitolDestroyed;
            Events.OnFinalBossDeafeted -= OnFinalBossDeafeted;
            Events.OnDistrictLimitReached -= OnDistrictLimitReached;
        }

        private void Update()
        {
            if (!checkForWinOnDeath) return;
            
            switch (winOnDeathSpawned)
            {
                case false when !winOnDeathQuery.IsEmpty:
                    winOnDeathSpawned = true;
                    break;
                case true when winOnDeathQuery.IsEmpty:
                    Events.OnFinalBossDeafeted?.Invoke();
                    break;
            }
        }

        private void OnDistrictLimitReached()
        {
            checkForWinOnDeath = true;
            Events.OnDistrictLimitReached -= OnDistrictLimitReached;
        }

        private void OnCapitolDestroyed(DistrictData destroyedDistrict)
        {
            DeafenEvents();
            InstantiateAfterDelay(gameOverPanelPrefab, gameOverDelay).Forget();
        }
        
        private void OnFinalBossDeafeted()
        {
            DeafenEvents();
            InstantiateAfterDelay(victoryPanelPrefab, victoryDelay).Forget();
        }

        private void DeafenEvents()
        {
            checkForWinOnDeath = false;
            Events.OnCapitolDestroyed -= OnCapitolDestroyed;
            Events.OnFinalBossDeafeted -= OnFinalBossDeafeted;
        }

        private async UniTaskVoid InstantiateAfterDelay(GameObject prefab, float delay)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delay));
            Instantiate(prefab);
        }
    }
}
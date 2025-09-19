using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Gameplay.Event;
using UnityEngine;
using Enemy.ECS;

namespace UI
{
    public class UIIncomingClusterHandler : MonoBehaviour
    {
        [SerializeField]
        private UIIncomingClusterDisplay displayPrefab;

        [SerializeField]
        private Transform displayContainer;
        
        private EntityManager entityManager;
        private EntityQuery incomingClusterQuery;
        
        private List<UIIncomingClusterDisplay> spawnedDisplays = new List<UIIncomingClusterDisplay>();

        private void Awake()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            incomingClusterQuery = entityManager.CreateEntityQuery(typeof(SpawningComponent));
        }

        private void OnEnable()
        {
            Events.OnTurnComplete += OnTurnComplete;
        }

        private void OnDisable()
        {
            Events.OnTurnComplete -= OnTurnComplete;
        }

        private void OnTurnComplete()
        {
            ReadSpawnData().Forget();
        }

        private async UniTaskVoid ReadSpawnData()
        {
            NativeList<SpawningComponent> array = incomingClusterQuery.ToComponentDataListAsync<SpawningComponent>(Allocator.TempJob, out var awaitJobHandle);
            awaitJobHandle.Complete();
            while (!awaitJobHandle.IsCompleted)
            {
                await UniTask.Yield();
            }

            if (!array.IsCreated) return;

            HideDisplays();

            if (array.Length == 0)
            {
                array.Dispose();
                return;
            }

            foreach (SpawningComponent data in array)
            {
                if (data.Turns <= 0)
                {
                    continue;
                }
                
                SpawnDisplay(data);
            }
            
            array.Dispose();
        }

        private void SpawnDisplay(SpawningComponent data)
        {
            UIIncomingClusterDisplay display = displayPrefab.Get<UIIncomingClusterDisplay>(displayContainer);
            display.Display(data);
            spawnedDisplays.Add(display);
        }

        private void HideDisplays()
        {
            for (int i = 0; i < spawnedDisplays.Count; i++)
            {
                spawnedDisplays[i].Hide();
            }
            
            spawnedDisplays.Clear();
        }
    }
}
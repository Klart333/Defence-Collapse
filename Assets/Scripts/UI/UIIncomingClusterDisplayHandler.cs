using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Enemy.ECS;
using Gameplay.Event;

namespace UI
{
    public class UIIncomingClusterDisplayHandler : MonoBehaviour
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
            Events.OnTurnIncreased += OnTurnIncreased;
        }

        private void OnDisable()
        {
            Events.OnTurnIncreased -= OnTurnIncreased;

            //incomingClusterQuery.Dispose();
        }

        private void OnTurnIncreased(int increased, int total)
        {
            DelayedTurnIncrease().Forget();
        }

        private async UniTaskVoid DelayedTurnIncrease()
        {
            await UniTask.NextFrame();
            
            NativeList<SpawningComponent> array = incomingClusterQuery.ToComponentDataListAsync<SpawningComponent>(Allocator.TempJob, out var awaitJobHandle);
            awaitJobHandle.Complete();
            while (!awaitJobHandle.IsCompleted)
            {
                await UniTask.Yield();
            }

            if (!array.IsCreated) return;

            if (array.Length == 0)
            {
                array.Dispose();
                return;
            }

            HideDisplays();
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
                spawnedDisplays[i].gameObject.SetActive(false);
            }
            
            spawnedDisplays.Clear();
        }
    }
}
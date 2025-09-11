using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Enemy.ECS;

namespace UI
{
    public class UIHighlightClusterHandler : MonoBehaviour 
    {
        // Needs to handle unhovering and re-hovering current highlighted cluster

        private EntityManager entityManager;
        private EntityQuery highlightClusterQuery;

        private bool reading;
        
        private void Awake()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            highlightClusterQuery = entityManager.CreateEntityQuery(typeof(HighlightClusterDataComponent));
        }

        private void Update()
        {
            if (reading)
            {
                return;
            }
            
            ReadHighlightClusterData().Forget();
        }
        
        private async UniTaskVoid ReadHighlightClusterData()
        {
            reading = true;
            
            NativeList<HighlightClusterDataComponent> array = highlightClusterQuery.ToComponentDataListAsync<HighlightClusterDataComponent>(Allocator.TempJob, out var awaitJobHandle);
            awaitJobHandle.Complete();
            while (!awaitJobHandle.IsCompleted)
            {
                await UniTask.Yield();
            }
            reading = false;

            if (!array.IsCreated) return;

            if (array.Length == 0)
            {
                array.Dispose();
                return;
            }
            
            if (array.Length > 1)
            {
                Debug.LogError("Should not exceed lenght 1!!!");
                array.Dispose();
                return;
            }

            foreach (HighlightClusterDataComponent cluster in array)
            {
                
            }
            
            entityManager.DestroyEntity(highlightClusterQuery);
            
            array.Dispose();
        }
    }
}
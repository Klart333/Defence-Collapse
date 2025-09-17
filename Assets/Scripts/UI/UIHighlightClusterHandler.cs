using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Enemy.ECS;
using Gameplay;

namespace UI
{
    public class UIHighlightClusterHandler : MonoBehaviour 
    {
        // Needs to handle unhovering and re-hovering current highlighted cluster

        [SerializeField]
        private 
        
        private GameManager gameManager;
        
        private EntityManager entityManager;
        private EntityQuery highlightClusterQuery;

        private bool reading;
        
        private void Awake()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            highlightClusterQuery = entityManager.CreateEntityQuery(typeof(HighlightClusterDataComponent));
            GetGameManager().Forget();
        }

        private async UniTaskVoid GetGameManager()
        {
            gameManager = await GameManager.Get();
        }

        private void Update()
        {
            if (reading || gameManager.IsGameOver)
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

            switch (array.Length)
            {
                case 0:
                    array.Dispose();
                    return;
                case > 1:
                    Debug.LogError("Should not exceed length 1!!!");
                    entityManager.DestroyEntity(highlightClusterQuery);
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
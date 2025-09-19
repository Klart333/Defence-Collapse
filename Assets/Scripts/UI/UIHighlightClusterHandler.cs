using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Entities;
using UnityEngine;
using InputCamera;
using Enemy.ECS;
using Gameplay;

namespace UI
{
    public class UIHighlightClusterHandler : MonoBehaviour 
    {

        [SerializeField]
        private UIHighlightClusterDisplay displayPrefab;

        [SerializeField]
        private RectTransform displayParent;
        
        private List<UIHighlightClusterDisplay> spawnedDisplays = new List<UIHighlightClusterDisplay>();
        
        private GameManager gameManager;
        private InputManager inputManager;
        
        private HighlightClusterDataComponent? lastData;
        private EntityQuery highlightClusterQuery;
        private EntityManager entityManager;
        
        private bool displaying;
        
        private void Awake()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            highlightClusterQuery = entityManager.CreateEntityQuery(typeof(HighlightClusterDataComponent));
            
            GetGameManager().Forget();
            GetInput().Forget();
        }

        private async UniTaskVoid GetInput()
        {
            inputManager = await InputManager.Get();
        }
        
        private async UniTaskVoid GetGameManager()
        {
            gameManager = await GameManager.Get();
        }

        private void Update()
        {
            if (inputManager == null || gameManager.IsGameOver || InputManager.MouseOverUI())
            {
                return;
            }
            
            ReadHighlightClusterData();

            HandleDisplaying();
        }
        
        private void ReadHighlightClusterData()
        {
            int entityCount = highlightClusterQuery.CalculateEntityCount();
            switch (entityCount)
            {
                case 0:
                    return;
                case > 1:
                    entityManager.DestroyEntity(highlightClusterQuery);
                    return;
            }

            if (!highlightClusterQuery.TryGetSingleton(out HighlightClusterDataComponent highlightData)) return;
            
            lastData = highlightData;
            if (displaying)
            {
                HideDisplays();
                Display(highlightData);
            }
            
            entityManager.DestroyEntity(highlightClusterQuery);
        }
        
        private void HandleDisplaying()
        {
            if (!lastData.HasValue)
            {
                return;
            }

            if (inputManager.CurrentMousePathIndex.Equals(lastData.Value.PathIndex))
            {
                if (!displaying)
                {
                    Display(lastData.Value);
                }
            }
            else if (displaying)
            {
                HideDisplays();
            }
        }
        
        private void Display(HighlightClusterDataComponent highlightData)
        {
            displaying = true;
            
            UIHighlightClusterDisplay display = displayPrefab.Get<UIHighlightClusterDisplay>(displayParent);
            display.Display(highlightData);
            spawnedDisplays.Add(display);
        }

        private void HideDisplays()
        {
            displaying = false;

            for (int i = 0; i < spawnedDisplays.Count; i++)
            {
                spawnedDisplays[i].Hide();
            }
            
            spawnedDisplays.Clear();
        }
    }
}
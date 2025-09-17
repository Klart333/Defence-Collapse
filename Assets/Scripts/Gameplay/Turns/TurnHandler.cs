using Gameplay.Turns.ECS;
using Gameplay.Event;
using Unity.Entities;
using UnityEngine;

namespace Gameplay.Turns
{
    public class TurnHandler : MonoBehaviour
    {
        private Entity turnIncreaseEntityPrefab;
        private EntityManager entityManager;
        private EntityQuery turnQuery;

        private bool listeningForTurnComplete;
        
        public int Turn { get; private set; }

        private void OnEnable()
        {
            Events.OnTurnIncreased += OnTurnIncreased;
            Events.OnTurnComplete += OnTurnComplete;

            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            turnQuery = entityManager.CreateEntityQuery(typeof(TurnIncreaseComponent));
            CreateEntityPrefab();
        }

        private void OnDisable()
        {
            Events.OnTurnIncreased -= OnTurnIncreased;
            Events.OnTurnComplete -= OnTurnComplete;
        }

        private void Update()
        {
            if (listeningForTurnComplete && turnQuery.IsEmpty)
            {
                listeningForTurnComplete = false;
                Events.OnTurnComplete?.Invoke();
            }
        }

        private void CreateEntityPrefab()
        {
            ComponentType[] archetype = {
                typeof(TurnIncreaseComponent),
                typeof(Prefab),
            };
            turnIncreaseEntityPrefab = entityManager.CreateEntity(archetype);
        }

        private void OnTurnIncreased(int increase, int total)
        {
            listeningForTurnComplete = true;
            Turn = total;

            Entity spawned = entityManager.Instantiate(turnIncreaseEntityPrefab);
            entityManager.SetComponentData(spawned, new TurnIncreaseComponent
            {
                TotalTurn = total,
                TurnIncrease = increase,
            });
        }
        
        private void OnTurnComplete()
        {
            PersistantGameStats.CurrentPersistantGameStats.TurnCount++;
        }
    }
}
using Gameplay.Turns.ECS;
using Unity.Entities;
using UnityEngine;

namespace Gameplay.Turns
{
    public class TurnHandler : MonoBehaviour
    {
        private EntityManager entityManager;
        private Entity turnIncreaseEntityPrefab;
        
        public int Turn { get; set; }

        private void OnEnable()
        {
            Events.OnTurnIncreased += OnTurnIncreased;

            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            CreateEntityPrefab();
        }

        private void OnDisable()
        {
            Events.OnTurnIncreased -= OnTurnIncreased;
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
            Turn = total;

            Entity spawned = entityManager.Instantiate(turnIncreaseEntityPrefab);
            entityManager.SetComponentData(spawned, new TurnIncreaseComponent
            {
                TotalTurn = total,
                TurnIncrease = increase,
            });
        }
    }
}
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Gameplay.Turns.ECS;
using Unity.Mathematics;
using Gameplay.Event;
using Unity.Entities;
using UnityEngine;
using System;
using Saving;

namespace Gameplay.Turns
{
    public class TurnHandler : MonoBehaviour
    {
        public event Action OnTurnAmountChanged;
        
        [Title("References")]
        [SerializeField]
        private TurnRewardHandler turnRewardHandler;
        
        private GameManager gameManager;
        private PersistantSaveManager persistantSaveManager;
        
        private Entity turnIncreaseEntityPrefab;
        private EntityManager entityManager;
        private EntityQuery turnQuery;

        private bool listeningForTurnComplete;
        private int turnAmountToComplete;
        
        public int Turn { get; private set; }
        public int TurnAmount { get; set; } = 1;

        private void OnEnable()
        {
            Events.OnTurnSequenceStarted += StartTurnSequence;
            Events.OnTurnComplete += OnTurnComplete;

            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            turnQuery = entityManager.CreateEntityQuery(typeof(TurnIncreaseComponent));
            CreateEntityPrefab();
            
            GetGameManager().Forget();
        }

        private void OnDisable()
        {
            Events.OnTurnSequenceStarted -= StartTurnSequence;
            Events.OnTurnComplete -= OnTurnComplete;
        }
        
        private async UniTaskVoid GetGameManager()
        {
            gameManager = await GameManager.Get();
        }

        private void Update()
        {
            if (gameManager.IsGameOver) return;
            
            if (!listeningForTurnComplete && turnAmountToComplete > 0)
            {
                IncreaseTurn();
                return;
            }
            
            if (!listeningForTurnComplete || !turnQuery.IsEmpty) return;

            CompleteTurn();
        }

        private void CreateEntityPrefab()
        {
            ComponentType[] archetype = {
                typeof(TurnIncreaseComponent),
                typeof(Prefab),
            };
            turnIncreaseEntityPrefab = entityManager.CreateEntity(archetype);
        }

        private void CompleteTurn()
        {
            turnAmountToComplete--;
            listeningForTurnComplete = false;
            
            Events.OnTurnComplete?.Invoke();

            if (turnAmountToComplete == 0)
            {
                Events.OnTurnSequenceCompleted?.Invoke();
            }
        }

        private void StartTurnSequence()
        {
            turnAmountToComplete = TurnAmount;
            
            IncreaseTurn();
        }
        
        private void IncreaseTurn()
        {
            listeningForTurnComplete = true;
            Turn++;

            Entity spawned = entityManager.Instantiate(turnIncreaseEntityPrefab);
            entityManager.SetComponentData(spawned, new TurnIncreaseComponent
            {
                TotalTurn = Turn,
                TurnIncrease = 1,
            });
            
            Events.OnTurnIncreased?.Invoke(1, Turn);
        }
        
        private void OnTurnComplete()
        {
            PersistantGameStats.CurrentPersistantGameStats.TurnCount++;
        }

        public void ChangeTurnAmount(int amount)
        {
            TurnAmount = math.clamp(TurnAmount + amount, 1, turnRewardHandler.MaxTurnAmount);
            OnTurnAmountChanged?.Invoke();
        }
        
        public void SetTurnAmount(int amount)
        {
            TurnAmount = math.clamp(amount, 1, turnRewardHandler.MaxTurnAmount);
            OnTurnAmountChanged?.Invoke();
        }
    }
}
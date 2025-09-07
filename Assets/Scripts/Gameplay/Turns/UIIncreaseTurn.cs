using Gameplay.Event;
using UnityEngine.UI;
using UnityEngine;

namespace Gameplay.Turns
{
    public class UIIncreaseTurn : MonoBehaviour
    {
        [SerializeField]
        private Button increaseTurnButton;
        
        private TurnHandler turnHandler;

        private bool isProcessingTurn;
        
        private void Awake()
        {
            turnHandler = FindFirstObjectByType<TurnHandler>();
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
            isProcessingTurn = false;
            increaseTurnButton.interactable = !isProcessingTurn;
        }

        public void IncreaseTurn()
        {
            Events.OnTurnIncreased?.Invoke(1, turnHandler.Turn + 1);
            
            isProcessingTurn = true;
            //increaseTurnButton.interactable = !isProcessingTurn;
        }
    }
}
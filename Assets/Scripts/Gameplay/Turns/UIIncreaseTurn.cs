using Cysharp.Threading.Tasks;
using UnityEngine.InputSystem;
using Gameplay.Event;
using UnityEngine.UI;
using InputCamera;
using UnityEngine;

namespace Gameplay.Turns
{
    public class UIIncreaseTurn : MonoBehaviour
    {
        [SerializeField]
        private Button increaseTurnButton;
        
        private TurnHandler turnHandler;
        private InputManager inputManager;

        private bool isProcessingTurn;
        
        private void Awake()
        {
            turnHandler = FindFirstObjectByType<TurnHandler>();
        }

        private void OnEnable()
        {
            Events.OnTurnComplete += OnTurnComplete;
            Events.OnTurnIncreased += OnTurnIncreased;
            
            GetInput().Forget();
        }

        private void OnDisable()
        {
            Events.OnTurnComplete -= OnTurnComplete;
            Events.OnTurnIncreased -= OnTurnIncreased;
            
            if (inputManager != null)
            {
                inputManager.Space.performed -= SpacePerformed;
            }
        }
        
        private async UniTaskVoid GetInput()
        {
            inputManager = await InputManager.Get();
            inputManager.Space.performed += SpacePerformed;
        }

        private void SpacePerformed(InputAction.CallbackContext obj)
        {
            if (isProcessingTurn)
            {
                return;
            }
            
            IncreaseTurn();    
        }

        public void IncreaseTurn()
        {
            Events.OnTurnIncreased?.Invoke(1, turnHandler.Turn + 1);
        }
        
        private void OnTurnIncreased(int arg0, int arg1)
        {
            isProcessingTurn = true;
            increaseTurnButton.interactable = !isProcessingTurn;
        }
        
        private void OnTurnComplete()
        {
            isProcessingTurn = false;
            increaseTurnButton.interactable = !isProcessingTurn;
        }
    }
}
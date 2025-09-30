using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using TMPro;

namespace Gameplay.Turns
{
    public class UITurnAmountDisplay : MonoBehaviour
    {
        [Title("References")]
        [SerializeField]
        private TurnHandler turnHandler;
        
        [Title("Setup")]
        [SerializeField]
        private TextMeshProUGUI amountText;

        [Title("Settings")]
        [SerializeField]
        private int minTurnAmount = 1;
        
        [SerializeField]
        private int maxTurnAmount = 10;
        
        private int turnAmount = 1;

        private void OnEnable()
        {
            turnAmount = minTurnAmount;
            turnHandler.TurnAmount = turnAmount;
        }

        public void IncreaseTurnAmount()
        {
            turnAmount = math.clamp(turnAmount + 1, minTurnAmount, maxTurnAmount);
            amountText.text = turnAmount.ToString();
            
            turnHandler.TurnAmount = turnAmount;
        }

        public void DecreaseTurnAmount()
        {
            turnAmount = math.clamp(turnAmount - 1, minTurnAmount, maxTurnAmount);
            amountText.text = turnAmount.ToString();
            
            turnHandler.TurnAmount = turnAmount;
        }
    }
}
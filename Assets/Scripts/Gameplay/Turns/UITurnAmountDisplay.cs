using Sirenix.OdinInspector;
using UnityEngine;
using TMPro;

namespace Gameplay.Turns
{
    public class UITurnAmountDisplay : MonoBehaviour
    {
        [Title("References")]
        [SerializeField]
        private TurnHandler turnHandler;
        
        [SerializeField]
        private TurnRewardHandler turnRewardHandler;
        
        [Title("Setup")]
        [SerializeField]
        private TextMeshProUGUI amountText;
        
        private void OnEnable()
        {
            turnHandler.OnTurnAmountChanged += TurnAmountChanged;
        }

        private void OnDisable()
        {
            turnHandler.OnTurnAmountChanged -= TurnAmountChanged;
        }

        private void TurnAmountChanged()
        {
            int turnAmount = turnHandler.TurnAmount;
            amountText.text = turnAmount.ToString();
            
        }

        public void IncreaseTurnAmount()
        {
            turnHandler.ChangeTurnAmount(1);
        }

        public void DecreaseTurnAmount()
        {
            turnHandler.ChangeTurnAmount(-1);
        }
    }
}
using Sirenix.OdinInspector;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

namespace Buildings.Barricades
{
    public class UIBarricadeButton : MonoBehaviour
    {
        [Title("Setup")]
        [SerializeField]
        private TextMeshProUGUI amountText;

        [SerializeField]
        private Button button;

        [SerializeField]
        private GameObject disabledOverlay;
        
        private BarricadeHandler barricadeHandler;

        private void OnEnable()
        {
            barricadeHandler = FindFirstObjectByType<BarricadeHandler>();
            barricadeHandler.OnAvailableBarricadesChanged += UpdateAvailable;
            
            UpdateAvailable();
        }

        private void OnDisable()
        {
            barricadeHandler.OnAvailableBarricadesChanged -= UpdateAvailable;
        }

        private void UpdateAvailable()
        {
            SetInteractable();
            UpdateAmountText();
        }

        private void SetInteractable()
        {
            bool isInteractable = barricadeHandler.AvailableBarriers > 0;
            button.interactable = isInteractable;
            disabledOverlay.SetActive(!isInteractable);
        }

        private void UpdateAmountText()
        {
            amountText.text = $"{barricadeHandler.AvailableBarriers:N0}";
        }
    }
}
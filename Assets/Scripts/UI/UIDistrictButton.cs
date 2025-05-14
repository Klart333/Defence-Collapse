using Sirenix.OdinInspector;
using Buildings.District;
using UnityEngine;
using Gameplay;
using TMPro;

namespace UI
{
    public class UIDistrictButton : MonoBehaviour
    {
        [Title("Cost")]
        [SerializeField]
        private TextMeshProUGUI costText;
        
        [SerializeField]
        private DistrictType districtType;
        
        [SerializeField]
        private DistrictCostUtility costUtility;

        private DistrictHandler districtHandler;

        private void Awake()
        {
            districtHandler = FindFirstObjectByType<DistrictHandler>();
        }

        private void OnEnable()
        {
            Events.OnDistrictBuilt += OnDistrictBuilt;
            
            UpdateCostText();
        }

        private void OnDisable()
        {
            Events.OnDistrictBuilt -= OnDistrictBuilt;
        }

        private void OnDistrictBuilt(DistrictType arg0)
        {
            UpdateCostText();
        }

        private void UpdateCostText()
        {
            int amount = districtHandler.GetDistrictAmount(districtType);
            float cost = costUtility.GetCost(districtType, amount);
            costText.text = $"{cost:N0}g";
        }
    }
}
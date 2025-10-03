using Sirenix.OdinInspector;
using Buildings.District;
using UnityEngine;
using Gameplay;
using TMPro;

namespace Buildings.District.UI
{
    public class UIDistrictButton : MonoBehaviour
    {
        [Title("Reference")]
        [SerializeField]
        private UIDistrictIcon districtIcon;
        
        [Title("Cost")]
        [SerializeField]
        private TextMeshProUGUI costText;
        
        [SerializeField]
        private DistrictCostUtility costUtility;

        private DistrictHandler districtHandler;

        private DistrictType districtType;

        private void OnDisable()
        {
            districtHandler.OnDistrictAmountChanged -= UpdateCostText;
        }

        public void Setup(DistrictHandler districtHandler, TowerData towerData)
        {
            this.districtHandler = districtHandler;
            
            districtType = towerData.DistrictType;
            
            districtIcon.DisplayDistrict(towerData, null);
            
            UpdateCostText();

            districtHandler.OnDistrictAmountChanged += UpdateCostText;
        }

        private void UpdateCostText()
        {
            int amount = districtHandler.GetDistrictAmount(districtType);
            float cost = costUtility.GetCost(districtType, amount);
            costText.text = $"{cost:N0}g";
        }
    }
}
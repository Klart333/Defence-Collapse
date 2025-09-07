using Sirenix.OdinInspector;
using Buildings.District;
using UnityEngine.UI;
using UnityEngine;
using Gameplay;
using Gameplay.Event;
using TMPro;

namespace UI
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

        private void OnEnable()
        {
            Events.OnDistrictBuilt += OnDistrictBuilt;
        }

        private void OnDisable()
        {
            Events.OnDistrictBuilt -= OnDistrictBuilt;
        }

        public void Setup(DistrictHandler districtHandler, TowerData towerData)
        {
            this.districtHandler = districtHandler;
            
            districtType = towerData.DistrictType;
            
            districtIcon.DisplayDistrict(towerData, null);
            
            UpdateCostText();
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
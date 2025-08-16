using Sirenix.OdinInspector;
using Buildings.District;
using UnityEngine.UI;
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
        private Image iconImage;

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
            iconImage.sprite = towerData.Icon;

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
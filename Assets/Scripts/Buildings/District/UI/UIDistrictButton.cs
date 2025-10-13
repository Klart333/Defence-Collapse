using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Gameplay.Money;
using UnityEngine.UI;
using UnityEngine;
using Gameplay;
using TMPro;

namespace Buildings.District.UI
{
    public class UIDistrictButton : MonoBehaviour
    {
        [Title("Setup")]
        [SerializeField]
        private UIDistrictIcon districtIcon;

        [SerializeField]
        private Button button;
        
        [SerializeField]
        private GameObject disabledOverlay;
        
        [Title("Cost")]
        [SerializeField]
        private TextMeshProUGUI costText;
        
        [SerializeField]
        private DistrictCostUtility costUtility;

        private DistrictHandler districtHandler;
        private MoneyManager moneyManager;

        private DistrictType districtType;

        private float cost; 

        private void OnEnable()
        {
            GetMoney().Forget();
        }

        private async UniTaskVoid GetMoney()
        {
            moneyManager = await MoneyManager.Get();
            moneyManager.OnMoneyChanged += OnMoneyChanged;
        }

        private void OnDisable()
        {
            districtHandler.OnDistrictAmountChanged -= UpdateCostText;
            
            if (moneyManager)
            {
                moneyManager.OnMoneyChanged -= OnMoneyChanged;
            }
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
            cost = costUtility.GetCost(districtType, amount);
            costText.text = $"{cost:N0}g"; 
             
            UpdateInteractable();
        }

        private void UpdateInteractable()
        {
            bool isAfforable = (moneyManager ?? MoneyManager.Instance).Money >= cost;
            button.interactable = isAfforable;
            disabledOverlay.SetActive(!isAfforable);
            
        }
        
        private void OnMoneyChanged(float _)
        {
            UpdateInteractable();
        }
    }
}
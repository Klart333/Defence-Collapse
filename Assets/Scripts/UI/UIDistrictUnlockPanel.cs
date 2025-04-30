using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UIDistrictUnlockPanel : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI titleText;
        
        [SerializeField]
        private Image iconImage;
        
        [SerializeField]
        private TextMeshProUGUI descriptionText;

        private Action<TowerData> clickCallback;
        private TowerData towerData;
        
        public void DisplayDistrict(TowerData towerData, Action<TowerData> callback)
        {
            titleText.text = towerData.DistrictName;
            iconImage.sprite = towerData.Icon;
            descriptionText.text = towerData.Description;
            
            this.towerData = towerData;
            clickCallback = callback;
        }

        public void OnClick()
        {
            clickCallback?.Invoke(towerData);
        }   
    }
}
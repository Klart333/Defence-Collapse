using System;
using Buildings.District;
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
        private Image smallIconImage;
        
        private Action<TowerData> clickCallback;
        private TowerData towerData;
        
        public void DisplayDistrict(TowerData towerData, Action<TowerData> callback)
        {
            titleText.text = towerData.DistrictName;
            iconImage.sprite = towerData.Icon;
            smallIconImage.sprite = towerData.IconSmall;
            
            this.towerData = towerData;
            clickCallback = callback;
        }

        public void OnClick()
        {
            clickCallback?.Invoke(towerData);
        }   
    }
}
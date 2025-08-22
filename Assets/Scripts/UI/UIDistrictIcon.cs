using System.Collections.Generic;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;
using Buildings.District;
using UnityEngine.UI;
using UnityEngine;
using System;
using TMPro;

namespace UI
{
    public class UIDistrictIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Title("References")]
        [SerializeField]
        private TextMeshProUGUI titleText;
        
        [SerializeField]
        private Image iconImage;

        [SerializeField]
        private Image smallIconImage;

        [Title("Tooltip")]
        [SerializeField]
        private float heightOffset = 10;
        
        private Action<TowerData> clickCallback;
        
        private UITooltipHandler tooltipHandler;
        private RectTransform rectTransform;
        private TowerData towerData;
        private Canvas canvas;
        
        private void Awake()
        {
            tooltipHandler = FindFirstObjectByType<UITooltipHandler>();
            rectTransform = transform as RectTransform;
            canvas = transform.GetComponentInParent<Canvas>();
        }

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
            if (clickCallback == null) return;
            
            clickCallback?.Invoke(towerData);
            tooltipHandler.ForceHideTooltips();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            List<Tuple<string, int>> districtDescription = new List<Tuple<string, int>>
            {
                Tuple.Create(towerData.DistrictName, 45),
                Tuple.Create(towerData.Description, 25),
            };
            
            Vector2 position = (Vector2)rectTransform.position + Vector2.up * (rectTransform.rect.height / 2.0f - heightOffset);
            position /= canvas.scaleFactor;
            tooltipHandler.DisplayTooltip(districtDescription, position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            tooltipHandler.PointerExitPanel();
        }
    }
}
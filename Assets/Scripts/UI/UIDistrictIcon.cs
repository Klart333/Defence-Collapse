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

        [SerializeField]
        private float requiredHoverTimer; 
        
        private Action<TowerData> clickCallback;
        
        private UITooltipHandler tooltipHandler;
        private RectTransform rectTransform;
        private TowerData towerData;
        private Canvas canvas;
        
        private float hoverTimer;
        private bool isHovering;
        private bool isDisplaying;
        
        private void Awake()
        {
            tooltipHandler = FindFirstObjectByType<UITooltipHandler>();
            rectTransform = transform as RectTransform;
            canvas = transform.GetComponentInParent<Canvas>();
        }

        private void Update()
        {
            if (isDisplaying || !isHovering)
            {
                return;
            }
            
            hoverTimer += Time.deltaTime;
            if (hoverTimer >= requiredHoverTimer)
            {
                DisplayTooltip();
            }
        }

        public void DisplayDistrict(TowerData towerData, Action<TowerData> callback)
        {
            if (titleText)
            {
                titleText.text = towerData.DistrictName;
            }

            if (iconImage)
            {
                iconImage.sprite = towerData.Icon;
            }

            if (smallIconImage)
            {
                smallIconImage.sprite = towerData.IconSmall;
            }
            
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
            isHovering = true;
            hoverTimer = 0;
            
            if (requiredHoverTimer <= 0)
            {
                DisplayTooltip();
            }
        }

        private void DisplayTooltip()
        {
            isDisplaying = true;
            
            List<Tuple<string, int>> districtDescription = new List<Tuple<string, int>>
            {
                Tuple.Create(towerData.DistrictName, 45),
                Tuple.Create(towerData.Description, 25),
            };
            
            Vector2 position = ToolTipUtility.GetTooltipPosition(rectTransform, canvas, heightOffset);
            tooltipHandler.DisplayTooltip(districtDescription, position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovering = false;

            if (isDisplaying)
            {
                tooltipHandler.PointerExitPanel();
            }
            
            isDisplaying = false;
        }
    }
}
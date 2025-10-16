using System.Collections.Generic;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;
using UnityEngine.UI;
using UnityEngine;
using System;
using Enemy;

namespace UI
{
    public class UIEnemyIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Title("References")]
        [SerializeField]
        private Image iconImage;

        [Title("Tooltip")]
        [SerializeField]
        private float heightOffset = 10;

        [SerializeField]
        private float requiredHoverTimer; 
        
        private UITooltipHandler tooltipHandler;
        private RectTransform rectTransform;
        private EnemyData enemyData;
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

        public void DisplayEnemy(EnemyData data)
        {
            if (iconImage)
            {
                iconImage.sprite = data.Icon;
            }

            enemyData = data;
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
            
            List<TextData> districtDescription = new List<TextData>
            {
                new TextData(enemyData.Name, 45),
                new TextData(enemyData.Description, 25),
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
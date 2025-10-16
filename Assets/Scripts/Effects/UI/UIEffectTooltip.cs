using System.Collections.Generic;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;
using UnityEngine;
using UI;

namespace Effects.UI
{
    public class UIEffectTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Title("Setup")]
        [SerializeField]
        private UIEffectDisplay effectDisplay;

        [Title("Tooltip Settings")]
        [SerializeField]
        private float heightOffset = 10;
        
        private UITooltipHandler tooltipHandler;
        private RectTransform rectTransform;
        private Canvas canvas; 

        private void Awake()
        {
            rectTransform = transform as RectTransform;
            
            tooltipHandler = FindFirstObjectByType<UITooltipHandler>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            DisplayTooltip();
        }

        private void DisplayTooltip()
        {
            canvas ??= GetComponentInParent<Canvas>();
            
            List<TextData> districtDescription = new List<TextData>
            {
                new TextData(effectDisplay.EffectModifier.Title, 20),
                new TextData(effectDisplay.EffectModifier.Description, 16),
            };
            
            Vector2 position = ToolTipUtility.GetTooltipPosition(rectTransform, canvas, heightOffset);
            tooltipHandler.DisplayTooltip(districtDescription, position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            tooltipHandler.PointerExitPanel();
        }
    }
}
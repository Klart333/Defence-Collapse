using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;
using Variables;
using System;

namespace UI
{
    public class UITooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField, TextArea]
        private StringReference[] tooltips;

        [SerializeField]
        private int[] tooltipSizes;

        [SerializeField]
        private float heightOffset = 10;
        
        private UITooltipHandler handler;
        private RectTransform rectTransform;
        private Canvas canvas;
        
        private void Awake()
        {
            handler = FindFirstObjectByType<UITooltipHandler>();
            rectTransform = GetComponent<RectTransform>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            List<Tuple<string, int>> description = new List<Tuple<string, int>>();
            for (int i = 0; i < tooltips.Length; i++)
            {
                description.Add(new Tuple<string, int>(tooltips[i].Value, tooltipSizes[i]));
            }
            
            Vector2 position = ToolTipUtility.GetTooltipPosition(rectTransform, canvas, heightOffset);
            handler.DisplayTooltip(description, position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            handler.PointerExitPanel();
        }
    }
}
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using System;
using UnityEngine.EventSystems;

namespace UI
{
    public class UITooltipHandler : MonoBehaviour
    {
        [Title("References")]
        [SerializeField]
        private UITooltipDisplay tooltipDisplayPrefab;
        
        [SerializeField]
        private Transform tooltipContainer;

        [Title("Settings")]
        [SerializeField]
        private float closeTime = 0.5f;
        
        private List<UITooltipDisplay> spawnedTooltips = new List<UITooltipDisplay>();
        
        private bool isHovering;
        private float closeTimer;

        private void Update()
        {
            if (isHovering || spawnedTooltips.Count <= 0) return;

            if (InputManager.Instance.Fire.WasPerformedThisFrame())
            {
                closeTimer = closeTime;
            }
            
            closeTimer += Time.deltaTime;
            if (closeTimer >= closeTime)
            {
                HideTooltips();
            }
        }

        public void DisplayTooltip(IEnumerable<Tuple<string, int>> tooltips, Vector2 position)
        {
            if (!isHovering && spawnedTooltips.Count > 0)
            {
                HideTooltips();
            }
            
            UITooltipDisplay tooltip = tooltipDisplayPrefab.Get<UITooltipDisplay>(tooltipContainer);
            (tooltip.transform as RectTransform).anchoredPosition = position;

            tooltip.DisplayTooltip(tooltips);
            tooltip.OnPointerEnter += PointerEnterPanel;
            tooltip.OnPointerExit += PointerExitPanel;
            
            spawnedTooltips.Add(tooltip);
            
            PointerEnterPanel();
        }

        private void PointerEnterPanel()
        {
            isHovering = true;
            closeTimer = 0;
        }

        public void PointerExitPanel()
        {
            isHovering = false;
            closeTimer = 0;
        }

        public void ForceHideTooltips() => HideTooltips(); // It's a bit stupid ik
        
        private void HideTooltips()
        {
            isHovering = false;
            
            foreach (UITooltipDisplay tooltip in spawnedTooltips)
            {
                tooltip.HideTooltip();
                tooltip.OnPointerEnter -= PointerEnterPanel;
                tooltip.OnPointerExit -= PointerExitPanel;
            }
            
            spawnedTooltips.Clear();
        }
    }

    public static class ToolTipUtility
    {
        public static Vector2 GetTooltipPosition(RectTransform rectTransform, Canvas canvas, float heightOffset)
        {
            Vector2 screenPosition;
            if (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera != null)
            {
                screenPosition = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, rectTransform.position);
            }
            else
            {
                screenPosition = (Vector2)rectTransform.position;
            }
            
            Vector2 position = screenPosition + Vector2.up * (rectTransform.rect.height / 2.0f - heightOffset);
            position /= canvas.scaleFactor;
            return position;
        }
    }
}
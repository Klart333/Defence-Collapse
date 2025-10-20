using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using System;
using InputCamera;
using UnityEngine.EventSystems;
using Utility;
using FocusType = Utility.FocusType;

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

        public void DisplayTooltip(ICollection<TextData> tooltips, Vector2 position, bool blocksRaycasts = false)
        {
            if (!isHovering && spawnedTooltips.Count > 0)
            {
                HideTooltips();
            }
            
            UITooltipDisplay tooltip = tooltipDisplayPrefab.Get<UITooltipDisplay>(tooltipContainer);
            (tooltip.transform as RectTransform).anchoredPosition = position;

            tooltip.DisplayTooltip(tooltips, blocksRaycasts);
            tooltip.OnPointerEnter += PointerEnterTooltipPanel;
            tooltip.OnPointerExit += PointerExitPanel;
            
            spawnedTooltips.Add(tooltip);
            
            PointerEnterPanel();
        }

        private void PointerEnterTooltipPanel()
        {
            if (FocusManager.Instance.GetIsFocused(out HashSet<Focus> focuses))
            {
                foreach (Focus focus in focuses)
                {
                    if (focus.FocusType == FocusType.Placing)
                    {
                        return;
                    }
                }
            }
            
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
                tooltip.HideTooltip().Forget();
                tooltip.OnPointerEnter -= PointerEnterPanel;
                tooltip.OnPointerExit -= PointerExitPanel;
            }
            
            spawnedTooltips.Clear();
        }
    }

    public struct TextData
    {
        public string Text;
        public float FontSize;

        public TextData(string text, float fontSize)
        {
            Text = text;
            FontSize = fontSize;
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
                screenPosition = rectTransform.position;
            }
            
            Vector2 position = screenPosition + Vector2.up * (rectTransform.rect.height / 2.0f - heightOffset);
            position /= canvas.scaleFactor;
            return position;
        }
    }
}
using System.Collections.Generic;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;
using UnityEngine.UI;
using System.Text;
using UnityEngine;
using DG.Tweening;
using Gameplay;
using System;
using UI;

namespace Exp.Gemstones
{
    public class UIGemstone : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    { 
        public event Action<Gemstone> OnClick;
        
        [Title("References")]
        [SerializeField]
        private Image icon;

        [SerializeField]
        private Image iconShadow;

        [SerializeField]
        private Sprite[] iconArray;
        
        [SerializeField]
        private Color[] stoneColorArray;
        
        [Title("Animation")]
        [SerializeField]
        private float hoverTargetScale = 1.1f;
        
        [SerializeField]
        private float hoverDuration = 0.2f;
        
        [SerializeField]
        private Ease hoverEase = Ease.OutElastic;

        [SerializeField]
        private Image whiteOutImage;
        
        [SerializeField]
        private float whiteOutDuration = 0.2f;
        
        [SerializeField]
        private Ease whiteOutEase = Ease.OutSine;
        
        private UITooltipHandler tooltipHandler;
        private RectTransform rectTransform;
        private Gemstone gemstone;

        private void Awake()
        {
            tooltipHandler = FindFirstObjectByType<UITooltipHandler>();
            rectTransform = transform as RectTransform;
        }

        public void DisplayGemstone(Gemstone gemstone)
        {
            this.gemstone = gemstone;
            
            Sprite gemstoneIcon = iconArray[(int)gemstone.GemstoneType];
            Color color = stoneColorArray[(int)gemstone.GemstoneType];
            icon.sprite = gemstoneIcon;
            icon.color = color;
            iconShadow.sprite = gemstoneIcon;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            transform.DOKill();
            transform.DOScale(hoverTargetScale, hoverDuration).SetEase(hoverEase).IgnoreGameSpeed(GameSpeedManager.Instance);
            
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < gemstone.Effects.Length; i++)
            {
                sb.AppendLine(gemstone.Effects[i].GetDescription());
            }
            
            List<Tuple<string, int>> gemstoneDescription = new List<Tuple<string, int>>
            {
                Tuple.Create($"{gemstone.GemstoneType.ToString()} Lvl. {gemstone.Level:N0}", 60),
                Tuple.Create(sb.ToString(), 30),
            };
            
            Vector2 position = rectTransform.anchoredPosition + Vector2.up * rectTransform.rect.height / 2.0f;
            tooltipHandler.DisplayTooltip(gemstoneDescription, position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.DOKill();
            transform.DOScale(1, hoverDuration).SetEase(hoverEase).IgnoreGameSpeed(GameSpeedManager.Instance);

            tooltipHandler.HideTooltip();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            whiteOutImage.gameObject.SetActive(true);
            whiteOutImage.DOFade(0, whiteOutDuration).SetEase(whiteOutEase).IgnoreGameSpeed(GameSpeedManager.Instance);
            
            tooltipHandler.HideTooltip();
            OnClick?.Invoke(gemstone);
        }
    }
}
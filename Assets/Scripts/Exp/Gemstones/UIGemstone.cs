using System.Collections.Generic;
using UnityEngine.EventSystems;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Gameplay.Event;
using UnityEngine.UI;
using System.Text;
using UnityEngine;
using DG.Tweening;
using Effects.UI;
using Gameplay;
using System;
using UI;
using UnityEngine.Serialization;

namespace Exp.Gemstones
{
    public class UIGemstone : MonoBehaviour, IDraggable, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IBeginDragHandler, IEndDragHandler
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

        [Title("Click")]
        [SerializeField]
        private bool isClickable = true;
        
        [SerializeField, ShowIf(nameof(isClickable))]
        private Image whiteOutImage;
        
        [SerializeField, ShowIf(nameof(isClickable))]
        private float whiteOutDuration = 0.2f;
        
        [SerializeField, ShowIf(nameof(isClickable))]
        private Ease whiteOutEase = Ease.OutSine;
        
        [Title("Drag")]
        [SerializeField]
        private bool isDraggable;

        [Title("Tooltip")]
        [SerializeField]
        private float heightOffset = 10;
        
        [SerializeField]
        private bool tooltipBlocksRaycasts = true;
        
        private UITooltipHandler tooltipHandler;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Canvas canvas;
        
        public IContainer Container { get; set; }
        public Gemstone Gemstone { get; private set; }

        private void Awake()
        {
            tooltipHandler = FindFirstObjectByType<UITooltipHandler>();
            rectTransform = transform as RectTransform;
            canvasGroup = GetComponent<CanvasGroup>();
            canvas = GetComponentInParent<Canvas>();
        }

        public void DisplayGemstone(Gemstone gemstone)
        {
            Gemstone = gemstone;
            
            Sprite gemstoneIcon = iconArray[(int)gemstone.GemstoneType];
            Color color = stoneColorArray[(int)gemstone.GemstoneType];
            icon.sprite = gemstoneIcon;
            icon.color = color;
            iconShadow.sprite = gemstoneIcon;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            transform.DOKill();
            transform.DOScale(hoverTargetScale, hoverDuration).SetEase(hoverEase);
            
            if (Gemstone == null) return;
            
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < Gemstone.Effects.Length; i++)
            {
                sb.AppendLine(Gemstone.Effects[i].GetDescription());
            }
            
            List<TextData> gemstoneDescription = new List<TextData>
            {
                new TextData($"{Gemstone.GemstoneType.ToString()} Lvl. {Gemstone.Level:N0}", 60),
                new TextData(sb.ToString(), 30),
            };

            Vector2 position = ToolTipUtility.GetTooltipPosition(rectTransform, canvas, heightOffset);
            tooltipHandler.DisplayTooltip(gemstoneDescription, position, tooltipBlocksRaycasts);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.DOKill();
            transform.DOScale(1, hoverDuration).SetEase(hoverEase);

            tooltipHandler.PointerExitPanel();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isClickable)
            {
                return;
            }
            
            whiteOutImage.gameObject.SetActive(true);
            whiteOutImage.DOFade(0, whiteOutDuration).SetEase(whiteOutEase);
            
            tooltipHandler.PointerExitPanel();
            OnClick?.Invoke(Gemstone);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!isDraggable)
            {
                return;
            }
            
            UIEvents.OnBeginDrag?.Invoke(this);
            
            tooltipHandler.PointerExitPanel();
            canvasGroup.blocksRaycasts = false;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDraggable)
            {
                return;
            }
            
            UIEvents.OnEndDrag?.Invoke(this);
            canvasGroup.blocksRaycasts = true;

            DelayedAdd().Forget();
        }

        private async UniTaskVoid DelayedAdd()
        {
            await UniTask.Yield();
            Container.AddDraggable(this);
        }

        public void SetParent(Transform parent)
        {
            transform.SetParent(parent);
        }
        
        public void Disable()
        {
            gameObject.SetActive(false);
        }
    }
}
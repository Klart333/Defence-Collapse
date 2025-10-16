using UnityEngine.EventSystems;
using Sirenix.OdinInspector;
using Gameplay.Event;
using UnityEngine.UI;
using UnityEngine;
using DG.Tweening;
using System;
using Loot;

namespace Effects.UI
{
    public class UIEffectDisplay : PooledMonoBehaviour, IDraggable, IClickable, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public event Action OnClick;
        
        [Title("References")]
        [SerializeField]
        private Image iconImage;

        [SerializeField]
        private Image frameImage;
        
        [Title("Animation", "Fade Frame")]
        [SerializeField]
        private float fadeFrameDuration = 0.2f;
        
        [SerializeField]
        private Ease fadeFrameEase = Ease.OutSine;

        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        private Canvas canvas;

        public IContainer Container { get; set; }
        public EffectModifier EffectModifier { get; private set; }

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            rectTransform = GetComponent<RectTransform>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            UIEvents.OnBeginDrag?.Invoke(this);

            transform.SetParent(canvas.transform);

            canvasGroup.blocksRaycasts = false;

            frameImage.DOKill();
            Color color = frameImage.color;
            color.a = 0;
            frameImage.color = color;
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Convert the drag delta to canvas scale
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, eventData.position, canvas.worldCamera, out Vector2 position);
            rectTransform.localPosition = position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            UIEvents.OnEndDrag?.Invoke(this);
            canvasGroup.blocksRaycasts = true;

            Container.AddDraggable(this);
            
            frameImage.DOKill();
            frameImage.DOFade(1f, fadeFrameDuration).SetEase(fadeFrameEase);
        }

        public void SetParent(Transform parent)
        {
            rectTransform.SetParent(parent);
            rectTransform.localScale = Vector3.one;
            rectTransform.anchoredPosition = Vector2.zero;
        }

        public void Display(EffectModifier effectModifier)
        {
            canvas ??= GetComponentInParent<Canvas>();

            EffectModifier = effectModifier;

            iconImage.sprite = effectModifier.Icon;
        }

        public void EffectClicked()
        {
            OnClick?.Invoke();
        }

        public void Disable()
        {
            gameObject.SetActive(false);
        }
    }
}

using UnityEngine.EventSystems;
using Sirenix.OdinInspector;
using Gameplay.Event;
using UnityEngine.UI;
using UnityEngine;
using System;
using Loot;

namespace Effects.UI
{
    public class UIEffectDisplay : PooledMonoBehaviour, IDraggable, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public event Action OnClick;
        
        [Title("References")]
        [SerializeField]
        private Image iconImage;

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
    }
}

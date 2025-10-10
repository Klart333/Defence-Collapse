using UnityEngine.EventSystems;
using Sirenix.OdinInspector;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine;

namespace Utility
{
    public class UIScaleOnHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Title("Setup")]
        [SerializeField]
        private RectTransform rectTransform;
        
        [SerializeField]
        private RectTransform layoutAffectedRect;

        [Title("Animation", "Scale")]
        [SerializeField]
        private float hoverScale;

        [SerializeField]
        private float hoverAnimationDuration = 0.3f;
        
        [SerializeField]
        private Ease hoverAnimationEase = Ease.OutSine;
        
        [Title("Animation", "Position")]
        [SerializeField]
        private Vector2 positionOffset;
        
        [SerializeField]
        private float positionAnimationDuration = 0.3f;
        
        [SerializeField]
        private Ease positionAnimationEase = Ease.OutSine;
        
        
        private float originalScale;
        private Vector2 originalPosition;
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            float currentScale = rectTransform.localScale.x;
            Vector2 currentPosition = rectTransform.anchoredPosition;
            rectTransform.DOComplete();
        
            originalScale = rectTransform.localScale.x;
            originalPosition = rectTransform.anchoredPosition;
            Vector2 targetPosition = rectTransform.anchoredPosition + positionOffset;
            
            rectTransform.localScale = Vector3.one * currentScale;
            rectTransform.anchoredPosition = currentPosition;

            rectTransform.DOAnchorPos(targetPosition, positionAnimationDuration).SetEase(positionAnimationEase);
            layoutAffectedRect.DOScale(hoverScale, hoverAnimationDuration).SetEase(hoverAnimationEase).onUpdate = () =>
            {
                LayoutRebuilder.MarkLayoutForRebuild(layoutAffectedRect);
            };;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            rectTransform.DOKill();
            rectTransform.DOAnchorPos(originalPosition, positionAnimationDuration).SetEase(positionAnimationEase);
            layoutAffectedRect.DOScale(originalScale, hoverAnimationDuration).SetEase(hoverAnimationEase).onUpdate = () =>
            {
                LayoutRebuilder.MarkLayoutForRebuild(layoutAffectedRect);
            };;
        }
    }
}
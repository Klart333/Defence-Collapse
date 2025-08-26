using UnityEngine.EventSystems;
using Sirenix.OdinInspector;
using DG.Tweening;
using UnityEngine;

namespace UI
{
    public class UIAnimateScaleOnHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Title("Animation")]
        [SerializeField]
        private float scaleMultiplier = 1.1f;
        
        [SerializeField]
        private float animationDuration = 0.2f;
        
        [SerializeField]
        private Ease animationEase = Ease.InOutSine;
        
        private Vector3 originalScale;
        private Vector3 targetScale;

        private void Awake()
        {
            originalScale = transform.localScale;
            targetScale = originalScale * scaleMultiplier;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            transform.DOKill();
            transform.DOScale(targetScale, animationDuration).SetEase(animationEase);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.DOKill();
            transform.DOScale(originalScale, animationDuration * 0.75f).SetEase(animationEase);
        }
    }
}
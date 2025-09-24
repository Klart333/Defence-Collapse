using UnityEngine.EventSystems;
using Sirenix.OdinInspector;
using UnityEngine.UI;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;
using Variables;

namespace UI
{
    public class UIDistrictFoldout : MonoBehaviour//, IPointerEnterHandler, IPointerExitHandler
    {
        [Title("Setup")]
        [SerializeField]
        private RectTransform foldoutTransform;

        [SerializeField]
        private Canvas canvas;
        
        [Title("Animation Settings", "Open")]
        [SerializeField]
        private float openDuration = 0.5f;
        
        [SerializeField]
        private Ease openEase = Ease.Linear;
        
        [SerializeField]
        private float closeDuration = 0.5f;
        
        [SerializeField]
        private Ease closeEase = Ease.Linear;

        [Title("Animation Settings", "Color")]
        [SerializeField]
        private Image[] highlightImages; 
        
        [SerializeField]
        private ColorReference defaultColor;
        
        [SerializeField]
        private ColorReference highlightColorTint;

        [SerializeField]
        private float colorAnimationDuration = 0.5f;
        
        [SerializeField]
        private Ease colorAnimationEase = Ease.InOutSine;
        
        public bool IsOpen { get; private set; }

        public void ToggleOpen(bool isOpen)
        {
            IsOpen = isOpen;
            if (isOpen)
            {
                OpenFoldout();
            }
            else
            {
                CloseFoldout();
            }
        }

        private void OpenFoldout()
        {
            for (int i = 2; i < foldoutTransform.childCount - 1; i++)
            {
                Transform element = foldoutTransform.GetChild(i);
                element.DOKill();
                element.gameObject.SetActive(true);
                element.DOScaleX(1.0f, openDuration).SetEase(openEase).onUpdate = () =>
                {
                    LayoutRebuilder.MarkLayoutForRebuild(foldoutTransform);
                };
            }
        }

        private void CloseFoldout()
        {
            for (int i = 2; i < foldoutTransform.childCount - 1; i++)
            {
                Transform element = foldoutTransform.GetChild(i);
                element.DOKill();
                TweenerCore<Vector3,Vector3,VectorOptions> tween = element.DOScaleX(0.0f, closeDuration + (i - 2.0f) * 0.03f).SetEase(closeEase);
                tween.onUpdate = () =>
                {
                    LayoutRebuilder.MarkLayoutForRebuild(foldoutTransform);
                };
                tween.onComplete = () =>
                {
                    element.gameObject.SetActive(false);
                };
            }
        }

        /*public void OnPointerEnter(PointerEventData eventData)
        {
            Color endColor = defaultColor.Value * highlightColorTint.Value;
            for (int i = 0; i < highlightImages.Length; i++)
            {
                highlightImages[i].DORewind();
                highlightImages[i].color = defaultColor.Value;
                highlightImages[i].DOColor(endColor, colorAnimationDuration).SetEase(colorAnimationEase).SetLoops(-1, LoopType.Yoyo);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            for (int i = 0; i < highlightImages.Length; i++)
            {
                highlightImages[i].DOKill();
                highlightImages[i].DOColor(defaultColor.Value, 0.2f).SetEase(colorAnimationEase);
            }
        }*/
    }
}
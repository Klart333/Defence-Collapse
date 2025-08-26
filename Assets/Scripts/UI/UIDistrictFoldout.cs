using UnityEngine.EventSystems;
using Sirenix.OdinInspector;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Variables;

namespace UI
{
    public class UIDistrictFoldout : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField]
        private RectTransform foldoutTransform;

        [SerializeField]
        private float headerButtonWidth = 120;
        
        [SerializeField]
        private float districtButtonWidth = 110;
        
        [SerializeField]
        private float buttonSpacing = 10;
        
        [SerializeField]
        private float padding = 10;

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
            foldoutTransform.DOKill();
            int buttonCount = 0;
            for (int i = 1; i < foldoutTransform.childCount; i++)
            {
                if (foldoutTransform.GetChild(i).gameObject.activeSelf)
                {
                    buttonCount++;
                }
            }
            
            float targetX = headerButtonWidth + buttonCount * (districtButtonWidth + buttonSpacing) + padding;
            foldoutTransform.DOAnchorPosX(targetX, openDuration).SetEase(openEase);
        }

        private void CloseFoldout()
        {
            foldoutTransform.DOKill();
            foldoutTransform.DOAnchorPosX(districtButtonWidth + buttonSpacing, closeDuration).SetEase(closeEase);
        }

        public void OnPointerEnter(PointerEventData eventData)
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
        }
    }
}
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using DG.Tweening;
using UnityEngine;
using Gameplay;

namespace UI
{
    public class UIDistrictFoldout : MonoBehaviour
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

        [Title("Animation Settings")]
        [SerializeField]
        private float openDuration = 0.5f;
        
        [SerializeField]
        private Ease openEase = Ease.Linear;
        
        [SerializeField]
        private float closeDuration = 0.5f;
        
        [SerializeField]
        private Ease closeEase = Ease.Linear;

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
            foldoutTransform.DOAnchorPosX(districtButtonWidth, closeDuration).SetEase(closeEase);
        }
    }
}
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

        [Title("Animation Settings")]
        [SerializeField]
        private float openDuration = 0.5f;
        
        [SerializeField]
        private Ease openEase = Ease.Linear;
        
        [SerializeField]
        private float closeDuration = 0.5f;
        
        [SerializeField]
        private Ease closeEase = Ease.Linear;

        [SerializeField]
        private bool useGameSpeed;
        
        private IGameSpeed gameSpeed;
        
        public bool IsOpen { get; private set; }

        private void Awake()
        {
            GetGameSpeed().Forget();
        }

        private async UniTaskVoid GetGameSpeed()
        {
            gameSpeed = await GameSpeedManager.Get();
        }

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
            
            float targetX = buttonCount * 140 + 5;
            Tween tween = foldoutTransform.DOAnchorPosX(targetX, openDuration).SetEase(openEase);
            if (!useGameSpeed)
            {
                tween.timeScale = 1.0f / gameSpeed.Value;
            }
        }

        private void CloseFoldout()
        {
            foldoutTransform.DOKill();
            Tween tween = foldoutTransform.DOAnchorPosX(0, closeDuration).SetEase(closeEase);
            if (!useGameSpeed)
            {
                tween.timeScale = 1.0f / gameSpeed.Value;
            }
        }
    }
}
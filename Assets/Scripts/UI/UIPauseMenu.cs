using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Gameplay;
using Sirenix.OdinInspector;
using UnityEngine.InputSystem;
using UnityEngine;

namespace UI
{
    public class UIPauseMenu : MonoBehaviour
    {
        [Title("Canvas")]
        [SerializeField]
        private Canvas pauseMenuCanvas;
        
        [Title("Animation")]
        [SerializeField]
        private RectTransform pauseMenuPanel;
        
        [SerializeField]
        private float duration = 0.2f;
        
        [SerializeField]
        private Ease ease = Ease.Linear;
        
        private InputManager inputManager;
        
        private GameSpeedManager gameSpeed;
        
        private bool isPaused;

        private void OnEnable()
        {
            GetInput().Forget();
            GetGameSpeed().Forget();
        }

        private void OnDisable()
        {
            inputManager.Escape.performed -= EscapePerformed;
        }

        private async UniTaskVoid GetGameSpeed()
        {
            gameSpeed = await GameSpeedManager.Get();
        }

        private async UniTaskVoid GetInput()
        {
            inputManager = await InputManager.Get();
            inputManager.Escape.performed += EscapePerformed;
        }

        private void EscapePerformed(InputAction.CallbackContext obj)
        {
            isPaused = !isPaused;

            if (isPaused)
            {
                OpenPauseMenu();  
            }
            else
            {
                ClosePauseMenu();
            }
        }

        public void OpenPauseMenu()
        {
            isPaused = true;
            gameSpeed.SetBaseGameSpeed(0, 0.2f);
            
            pauseMenuCanvas.gameObject.SetActive(true);
            pauseMenuPanel.DOAnchorPosX(0, duration).SetEase(ease);
        }

        public void ClosePauseMenu()
        {
            isPaused = false;
            gameSpeed.SetBaseGameSpeed(1, duration);
            
            pauseMenuPanel.DOKill();
            float width = pauseMenuPanel.rect.width + 20; // 20 for shadows
            pauseMenuPanel.DOAnchorPosX(-width, duration).SetEase(ease).onComplete += () =>
            {
                pauseMenuCanvas.gameObject.SetActive(false);
            };
        }
    }
}
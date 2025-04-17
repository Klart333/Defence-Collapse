using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Buildings.District;
using DG.Tweening;
using UnityEngine;

namespace Gameplay
{
    public class GameSpeedManager : Singleton<GameSpeedManager>, IGameSpeed
    {
        [Title("Game Speed")]
        [SerializeField]
        private float speedySpeed = 4.0f;

        [Title("Game Over")]
        [SerializeField]
        private float slowDownDuration = 1.0f;
        
        public float Value { get; private set; } = 1;
        
        private bool updatingGameSpeed = false;

        private async void OnEnable()
        {
            Events.OnCapitolDestroyed += OnCapitolDestroyed;
            Events.OnGameReset += OnGameReset;

            await UniTask.WaitUntil(() => InputManager.Instance != null);
            InputManager.Instance.Space.started += SpaceStarted;
            InputManager.Instance.Space.canceled += SpaceCanceled;
        }

        private void OnDisable()
        {
            Events.OnCapitolDestroyed -= OnCapitolDestroyed;
            Events.OnGameReset -= OnGameReset;
            
            InputManager.Instance.Space.started -= SpaceStarted;
            InputManager.Instance.Space.canceled -= SpaceCanceled;
        }

        private void Update()
        {
            if (!updatingGameSpeed && !Mathf.Approximately(DOTween.timeScale, Value))
            {
                DOTween.timeScale = Value;
            }
        }

        private void SpaceStarted(InputAction.CallbackContext obj)
        {
            Value *= speedySpeed;
        }

        private void SpaceCanceled(InputAction.CallbackContext obj)
        {
            Value /= speedySpeed;
        }

        private void OnCapitolDestroyed(DistrictData destroyedDistrict)
        {
            SetGameSpeed(0, slowDownDuration);
        }
        
        public void SetGameSpeed(float targetSpeed, float lerpDuration)
        {
            updatingGameSpeed = true;
            DOTween.To(() => Value, v => Value = v, targetSpeed, lerpDuration).SetEase(Ease.OutSine).SetUpdate(true).onComplete = () =>
            {
                updatingGameSpeed = false;
            };
        }
        
        public void SetGameSpeed(float targetSpeed)
        {
            Value = targetSpeed;
        }
        
        private void OnGameReset()
        {
            Value = 1;
            DOTween.timeScale = 1;
        }
    }
}
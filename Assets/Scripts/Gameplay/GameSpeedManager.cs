using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Buildings.District;
using Unity.Entities;
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
        
        private EntityManager entityManager;
        private Entity gameSpeedEntity;
        private Tween slowDownTween;
        
        private bool updatingGameSpeed = false;
        
        public float Value { get; private set; } = 1;

        private async void OnEnable()
        {
            Events.OnCapitolDestroyed += OnCapitolDestroyed;
            Events.OnGameReset += OnGameReset;
            
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            gameSpeedEntity = entityManager.CreateEntity(typeof(GameSpeedComponent));
            entityManager.AddComponentData(gameSpeedEntity, new GameSpeedComponent { Speed = Value });
            
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
            entityManager.SetComponentData(gameSpeedEntity, new GameSpeedComponent { Speed = Value });
        }

        private void SpaceCanceled(InputAction.CallbackContext obj)
        {
            Value /= speedySpeed;
            entityManager.SetComponentData(gameSpeedEntity, new GameSpeedComponent { Speed = Value });
        }

        private void OnCapitolDestroyed(DistrictData destroyedDistrict)
        {
            SetGameSpeed(0, slowDownDuration);
        }
        
        public void SetGameSpeed(float targetSpeed, float lerpDuration)
        {
            if (slowDownTween != null && slowDownTween.IsActive())
            {
                slowDownTween.Kill();
            }
            
            updatingGameSpeed = true;
            slowDownTween = DOTween.To(() => Value, v =>
            {
                Value = v;
                entityManager.AddComponentData(gameSpeedEntity, new GameSpeedComponent { Speed = Value });
            }, targetSpeed, lerpDuration).SetEase(Ease.OutSine).SetUpdate(true);
            
            slowDownTween.onComplete = () =>
            {
                updatingGameSpeed = false;
            };
        }
        
        public void SetGameSpeed(float targetSpeed)
        {
            Value = targetSpeed;
            entityManager.AddComponentData(gameSpeedEntity, new GameSpeedComponent { Speed = Value });
        }
        
        private void OnGameReset()
        {
            Value = 1;
            DOTween.timeScale = 1;
        }
    }

    public struct GameSpeedComponent : IComponentData
    {
        public float Speed;
    }
}
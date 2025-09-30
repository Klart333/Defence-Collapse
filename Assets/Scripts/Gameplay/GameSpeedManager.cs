using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Buildings.District;
using Unity.Entities;
using DG.Tweening;
using UnityEngine;
using Effects;
using Gameplay.Event;
using InputCamera;

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
        
        private InputManager inputManager;
        private Modifier speedUpModifier;
        private Tween slowDownTween;
        private Stat gameSpeedStat;
        
        private EntityManager entityManager;
        private Entity gameSpeedEntity;
        
        public float Value => gameSpeedStat.Value;

        protected override void Awake()
        {
            base.Awake();
            
            gameSpeedStat = new Stat(1);
            speedUpModifier = new Modifier
            {
                Value = speedySpeed,
                Type = Modifier.ModifierType.Multiplicative,
            };
        }

        private void OnEnable()
        {
            Events.OnCapitolDestroyed += OnCapitolDestroyed;
            Events.OnGameReset += OnGameReset;
            
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            gameSpeedEntity = entityManager.CreateEntity(typeof(GameSpeedComponent));
            entityManager.AddComponentData(gameSpeedEntity, new GameSpeedComponent { Speed = Value });
            
            GetInputManager().Forget();
        }

        private async UniTaskVoid GetInputManager()
        {
            inputManager = await InputManager.Get();
            inputManager.Shift.started += ShiftStarted;
            inputManager.Shift.canceled += ShiftCanceled;
        }

        private void OnDisable()
        {
            Events.OnCapitolDestroyed -= OnCapitolDestroyed;
            Events.OnGameReset -= OnGameReset;
            
            inputManager.Shift.started -= ShiftStarted;
            inputManager.Shift.canceled -= ShiftCanceled;
        }

        private void ShiftStarted(InputAction.CallbackContext obj)
        {
            gameSpeedStat.AddModifier(speedUpModifier);
            entityManager.SetComponentData(gameSpeedEntity, new GameSpeedComponent { Speed = Value });
        }

        private void ShiftCanceled(InputAction.CallbackContext obj)
        {
            gameSpeedStat.RemoveModifier(speedUpModifier);
            entityManager.SetComponentData(gameSpeedEntity, new GameSpeedComponent { Speed = Value });
        }
        
        private void OnCapitolDestroyed(DistrictData destroyedDistrict)
        {
            SetBaseGameSpeed(0.001f, slowDownDuration);
        }
        
        public void SetBaseGameSpeed(float targetSpeed, float lerpDuration)
        {
            if (slowDownTween != null && slowDownTween.IsActive())
            {
                slowDownTween.Kill();
            }
            
            slowDownTween = DOTween.To(() => gameSpeedStat.BaseValue, v =>
            {
                gameSpeedStat.BaseValue = v;
                entityManager.AddComponentData(gameSpeedEntity, new GameSpeedComponent { Speed = Value });
            }, targetSpeed, lerpDuration).SetEase(Ease.OutSine);
        }
        
        public void SetBaseGameSpeed(float targetSpeed)
        {
            gameSpeedStat.BaseValue = targetSpeed;
            entityManager.AddComponentData(gameSpeedEntity, new GameSpeedComponent { Speed = Value });
        }
        
        private void OnGameReset()
        {
            gameSpeedStat.BaseValue = 1;
            gameSpeedStat.RemoveAllModifiers();
        }

        public void AddModifier(Modifier modifier)
        {
            gameSpeedStat.AddModifier(modifier);
            entityManager.SetComponentData(gameSpeedEntity, new GameSpeedComponent { Speed = Value });
        }
    }

    public struct GameSpeedComponent : IComponentData
    {
        public float Speed;
    }
}
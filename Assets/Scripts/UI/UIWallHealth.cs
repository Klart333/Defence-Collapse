using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using UnityEngine.UI;
using UnityEngine;
using DG.Tweening;
using Gameplay;

namespace UI
{
    public class UIWallHealth : PooledMonoBehaviour
    {
        [Title("Fill Image")]
        [SerializeField]
        private Image fillImage;

        [Tooltip("The speed to drain 100%")]
        [SerializeField]
        private float fillSpeed = 1;
        
        [SerializeField]
        private Ease fillEase = Ease.Linear;
        
        [SerializeField]
        private Gradient fillGradient;
        
        [Title("Canvas Group")]
        [SerializeField]
        private CanvasGroup canvasGroup;
        
        [SerializeField]
        private float fadeInDuration = 0.3f;
        
        [SerializeField]
        private Ease fadeInEase = Ease.Linear;

        [Title("Fade out")]
        [SerializeField]
        private float fadeOutDelay = 5.0f;

        [Title("Danger")]
        [SerializeField]
        private CanvasGroup dangerGroup;
        
        [SerializeField]
        private Image dangerImage;

        [SerializeField]
        private float dangerThreshold = 0.3f;

        [SerializeField]
        private float dangerLoopDuration = 2f;
        
        [SerializeField]
        private Ease dangerEase = Ease.Linear;

        [Title("Mic")]
        [SerializeField]
        private Vector2 canvasPivot;

        private WallState wallState;
        private Vector3 targetPosition;
        private Canvas canvas;
        private Tween dangerTween;
        private Camera cam;

        private IGameSpeed gameSpeed;
            
        private float fadeOutTimer;
        private bool inDanger;

        private void Awake()
        {
            gameSpeed = GameSpeedManager.Instance;
            cam = Camera.main;
        }
        
        public void Setup(WallState wallData, float startHealth, Canvas canvas)
        {
            wallState = wallData;

            this.canvas = canvas;
            fillImage.fillAmount = startHealth / wallState.Health.MaxHealth;
            fillImage.color = fillGradient.Evaluate(fillImage.fillAmount);
            canvasGroup.DOFade(1, fadeInDuration).SetEase(fadeInEase);
            targetPosition = BuildingManager.Instance.GetPos(wallState.Index) + BuildingManager.Instance.GridScale / 2.0f;
            inDanger = wallState.Health.HealthPercentage < dangerThreshold;
            
            Events.OnBuiltIndexDestroyed += OnBuiltIndexDestroyed;
            wallState.Health.OnHealthChanged += OnHealthChanged;
            wallState.Health.OnDeath += OnDeath;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            Reset();
        }

        private void Reset()
        {
            fillImage.DOKill();
            canvasGroup.DOKill();
            dangerTween.Kill();
            canvasGroup.alpha = 0;
            dangerGroup.alpha = 0;
            fadeOutTimer = fadeOutDelay;
            
            if (wallState != null)
            {
                wallState.Health.OnHealthChanged -= OnHealthChanged;
                wallState.Health.OnDeath -= OnDeath;
                wallState = null;
            }
            
            Events.OnBuiltIndexDestroyed -= OnBuiltIndexDestroyed;
        }

        private void Update()
        {
            HandleDanger();
            
            PositionRectTransform.PositionOnOverlayCanvas(canvas, cam, transform as RectTransform, targetPosition, canvasPivot);

            fadeOutTimer -= Time.deltaTime * gameSpeed.Value;

            if (fadeOutTimer < 1.0f)
            {
                canvasGroup.alpha = fadeOutTimer;
            }

            if (fadeOutTimer <= 0)
            {
                gameObject.SetActive(false);
            }
        }

        private void HandleDanger()
        {
            if (inDanger)
            {
                float percent = 1.0f - (wallState.Health.HealthPercentage / dangerThreshold);
                Color col = dangerImage.color;
                col.a = percent;
                dangerImage.color = col;
                
                if (!dangerTween.IsActive())
                {
                    dangerGroup.gameObject.SetActive(true);
                    dangerTween = dangerGroup.DOFade(1, dangerLoopDuration).SetLoops(-1, LoopType.Yoyo).SetEase(dangerEase);
                }
            }
            else if (dangerTween.IsActive())
            {
                dangerTween.Kill();
                dangerGroup.gameObject.SetActive(false);
                dangerGroup.alpha = 0;
            }
        }

        private void OnBuiltIndexDestroyed(ChunkIndex chunkIndex)
        {
            if (wallState.Index.Equals(chunkIndex))
            {
                OnDeath();
            }
        }

        private void OnDeath()
        {
            gameObject.SetActive(false);
        }
        
        private void OnHealthChanged()
        {
            fadeOutTimer = fadeOutDelay;
            canvasGroup.alpha = 1;
            inDanger = wallState.Health.HealthPercentage < dangerThreshold;
            
            TweenFill();
        }

        public void TweenFill()
        {
            float percent = wallState.Health.HealthPercentage;
            float diff = fillImage.fillAmount - percent;
            float duration = diff / fillSpeed;
            fillImage.DOKill();
            
            DOTween.To(x => fillImage.color = fillGradient.Evaluate(1.0f - x), fillImage.fillAmount, percent, duration ).SetEase(fillEase);
            fillImage.DOFillAmount(percent, duration).SetEase(fillEase);
        }
    }
}
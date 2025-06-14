using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine.UI;
using UnityEngine;
using DG.Tweening;
using System;

namespace Juice
{
    public class SceneTransitionManager : Singleton<SceneTransitionManager>
    {
        public event Action OnSceneBeginChange; 
        public event Action<int> OnSceneLoaded; 
        
        [Title("References")]
        [SerializeField]
        private Canvas canvas;
        
        [SerializeField]
        private RectTransform transitionTransform;

        [SerializeField]
        private Image iconImage;
        
        [SerializeField]
        private Sprite[] iconSprites;

        [Title("Animation", "Slide In")]
        [SerializeField]
        private float slideInDuration = 0.3f;
        
        [SerializeField]
        private Ease slideInEase = Ease.OutSine;
        
        [Title("Animation", "Spin")]
        [SerializeField]
        private float spinDuation = 2f;
        
        [SerializeField]
        private Ease spinEase = Ease.OutSine;

        protected override void Awake()
        {
            base.Awake();

            Application.backgroundLoadingPriority = ThreadPriority.Low;
        }

        public async UniTaskVoid LoadScene(int index)
        {
            OnSceneBeginChange?.Invoke();
            
            await SlideInAnimation();
            FadeInAndSpinIcon();
            
            await SceneManager.LoadSceneAsync(index);
            
            OnSceneLoaded?.Invoke(index);
            
            SlideOutAnimation();
        }

        private void FadeInAndSpinIcon()
        {
            iconImage.sprite = iconSprites[Random.Range(0, iconSprites.Length)];
            iconImage.DOFade(1f, 0.4f);
            iconImage.transform.DOBlendableLocalRotateBy(new Vector3(0, 0, 360), spinDuation, RotateMode.FastBeyond360).SetEase(spinEase).SetLoops(-1, LoopType.Restart);
        }

        private async UniTask SlideInAnimation()
        {
            canvas.gameObject.SetActive(true);
            transitionTransform.DOAnchorPosX(0, slideInDuration).SetEase(slideInEase);
            await UniTask.Delay(TimeSpan.FromSeconds(slideInDuration));
        }

        private void SlideOutAnimation()
        {
            float width = transitionTransform.rect.width;
            iconImage.DOKill();
            iconImage.transform.rotation = Quaternion.identity;
            iconImage.DOFade(0f, 0.1f);
            transitionTransform.DOAnchorPosX(width, slideInDuration).SetEase(slideInEase).onComplete += () =>
            {
                canvas.gameObject.SetActive(false);
                transitionTransform.anchoredPosition = new Vector2(-width, 0);
            };
        }
    }
}
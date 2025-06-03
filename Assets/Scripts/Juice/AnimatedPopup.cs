using Sirenix.OdinInspector;
using DG.Tweening;
using UnityEngine;
using Gameplay;

namespace Juice
{
    public class AnimatedPopup : MonoBehaviour
    {
        [Title("Settings")]
        [SerializeField]
        private Ease easeType = Ease.Linear;
        
        [SerializeField, MinMaxRange(0, 3)]
        private RangedFloat lifetime = new RangedFloat(0, 3);

        [SerializeField]
        private bool useGameSpeed = true;

        [SerializeField]
        private bool readScale = false;

        [SerializeField, HideIf(nameof(readScale))]
        private Vector3 targetScale = Vector3.one;
        
        [Title("Debug")]
        [SerializeField]
        private bool verbose;
        
        private IGameSpeed gameSpeed;
        
        public Tween PopupTween { get; private set; }

        private void Awake()
        {
            gameSpeed = GameSpeedManager.Instance;
        }

        private void OnEnable()
        {
            Popup();
        }

        private void Popup()
        {
            Vector3 endValue = readScale ? transform.localScale : targetScale;
            
            transform.localScale = Vector3.zero;
            PopupTween = transform.DOScale(endValue, lifetime.Random()).SetEase(easeType);
            if (!useGameSpeed)
            {
                PopupTween.IgnoreGameSpeed(gameSpeed);
            }
        }

        private void OnDisable()
        {
            transform.DOComplete();
        }
    }
}
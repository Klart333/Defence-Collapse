using System;
using Sirenix.OdinInspector;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Gameplay;
using UnityEngine;

namespace Juice
{
    public class AnimatedPopup : MonoBehaviour
    {
        [Title("Settings")]
        [SerializeField]
        private Ease easeType = Ease.Linear;
        
        [SerializeField, MinMaxRange(0, 5)]
        private RangedFloat lifetime = new RangedFloat(0, 5);

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
            Vector3 endValue = readScale ? transform.localScale : targetScale;
            
            transform.localScale = Vector3.zero;
            PopupTween = transform.DOScale(endValue, lifetime.Random()).SetEase(easeType);
            if (!useGameSpeed)
            {
                PopupTween.timeScale = 1.0f / gameSpeed.Value;
            }
        }

        private void Update()
        {
            if (verbose)
            {
                Debug.Log(transform.localScale);
            }
        }

        private void OnDisable()
        {
            transform.DOComplete();
        }
    }
}
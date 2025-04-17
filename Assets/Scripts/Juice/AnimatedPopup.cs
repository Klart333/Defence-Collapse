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
        private bool readScale = false;

        [SerializeField]
        private bool useGameSpeed = true;
        
        private IGameSpeed gameSpeed;

        private void Awake()
        {
            gameSpeed = GameSpeedManager.Instance;
        }

        private void OnEnable()
        {
            Vector3 endValue = readScale ? transform.localScale : Vector3.one;
            
            transform.localScale = Vector3.one * 0.001f;
            TweenerCore<Vector3, Vector3, VectorOptions> tween = transform.DOScale(endValue, lifetime.Random()).SetEase(easeType);
            if (!useGameSpeed)
            {
                tween.timeScale = 1.0f / gameSpeed.Value;
            }
        }

        private void OnDisable()
        {
            transform.DOComplete();
        }
    }
}
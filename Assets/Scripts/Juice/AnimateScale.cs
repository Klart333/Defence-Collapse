using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Juice
{
    public class AnimateScale : MonoBehaviour
    {
        [Title("Settings")]
        [SerializeField]
        private Vector3 targetScale;

        [SerializeField]
        private bool readOriginScale = true;
        
        [SerializeField, HideIf(nameof(readOriginScale))]
        private Vector3 originScale;

        [Title("Animation")]
        [SerializeField]
        private float duration = 0.2f;
        
        [SerializeField]
        private Ease ease = Ease.InOutSine;

        private void Awake()
        {
            if (readOriginScale)
            {
                originScale = transform.localScale;
            }
        }

        public void AnimateToTargetScale()
        {
            transform.DOKill();
            transform.DOScale(targetScale, duration).SetEase(ease);
        }

        public void AnimateToOriginScale()
        {
            transform.DOKill();
            transform.DOScale(originScale, duration).SetEase(ease);
        }
    }
}
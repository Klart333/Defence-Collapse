using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Juice
{
    [RequireComponent(typeof(CanvasGroup))]
    public class AnimatedFadeIn : MonoBehaviour
    {
        [Title("Settings")]
        [SerializeField]
        private float fadeDuration = 0.5f;
        
        [SerializeField]
        private Ease fadeEase = Ease.OutSine;
        
        private CanvasGroup canvasGroup;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            canvasGroup.alpha = 0;
            canvasGroup.DOFade(1, fadeDuration).SetEase(fadeEase);
        }

        private void OnDisable()
        {
            canvasGroup.DOKill();
            canvasGroup.alpha = 0;
        }
    }
}
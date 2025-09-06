using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Gameplay.Upgrades;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UI
{
    public class UIHideOverlayHandler : MonoBehaviour
    {
        [Title("Setup")]
        [SerializeField]
        private CanvasGroup canvasGroup;

        [SerializeField]
        private Ease ease = Ease.InOutQuad;
        
        private void OnEnable()
        {
            Events.OnUpgradeCardsDisplayed += OnDisplayUpgrades;
            Events.OnUpgradeCardPicked += OnUpgradeCardPicked;
        }

        private void OnDisable()
        {
            Events.OnUpgradeCardsDisplayed -= OnDisplayUpgrades;
            Events.OnUpgradeCardPicked -= OnUpgradeCardPicked;
        }

        private void OnUpgradeCardPicked(UpgradeCardData.UpgradeCardInstance arg0)
        {
            FadeUI(0.3f, 1);
        }

        private void OnDisplayUpgrades()
        {
            FadeUI(0.1f, 0);
        }

        private void FadeUI(float duration, float target)
        {
            canvasGroup.DOKill();
            canvasGroup.DOFade(target, duration).SetEase(ease);
        }
    }
}
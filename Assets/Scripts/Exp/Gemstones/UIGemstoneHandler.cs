using Sirenix.OdinInspector;
using Gameplay.GameOver;
using DG.Tweening;
using Gameplay;
using UnityEngine;

namespace Exp.Gemstones
{
    public class UIGemstoneHandler : MonoBehaviour
    {
        [Title("UI")]
        [SerializeField]
        private UIGemstone[] gemstones;
        
        [SerializeField]
        private CanvasGroup canvasGroup;
        
        [Title("Animation")]
        [SerializeField]
        private float fadeInDuration = 0.5f;
        
        [SerializeField]
        private Ease fadeInEase = Ease.OutCirc;
        
        [SerializeField]
        private float fadeOutDuration = 0.5f;
        
        [SerializeField]
        private Ease fadeOutEase = Ease.OutCirc;

        [Title("Gemstone")]
        [SerializeField]
        private AnimationCurve expToLevelCurve;
        
        [SerializeField]
        private GemstoneGenerator gemstoneGenerator;
        
        public void SpawnGemstones()
        {
            gameObject.SetActive(true);
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.DOFade(1, fadeInDuration).SetEase(fadeInEase).IgnoreGameSpeed(GameSpeedManager.Instance).onComplete = () => canvasGroup.interactable = true;
            
            int exp = FindFirstObjectByType<UIGameOverExpHandler>().ExpGained;
            int level = (int)expToLevelCurve.Evaluate(exp);
            for (int i = 0; i < 3; i++)
            {
                int seed = Random.Range(0, int.MaxValue);

                Gemstone gemstone = gemstoneGenerator.GetGemstone((GemstoneType)i, level, seed);
                gemstones[i].DisplayGemstone(gemstone);
                gemstones[i].OnClick += SelectGemstone;
            }
        }

        public void SelectGemstone(Gemstone gemstone)
        {
            canvasGroup.DOKill();
            canvasGroup.DOFade(0, fadeOutDuration).SetEase(fadeOutEase).IgnoreGameSpeed(GameSpeedManager.Instance).onComplete = () => gameObject.SetActive(false);
            ExpManager.Instance.AddGemstone(gemstone);
        }
    }
}
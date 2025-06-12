using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using DG.Tweening;
using UnityEngine;

namespace Gameplay.Upgrades
{
    public class UpgradeCardHandler : MonoBehaviour
    {
        [Title("References")]
        [SerializeField]
        private GameObject upgradeCardParent;
        
        [SerializeField]
        private UIUpgradeCard[] upgradeCards;
        
        [SerializeField]
        private UpgradeCardDataUtility upgradeDataUtility;

        [Title("Animation")]
        [SerializeField]
        private float fadeInDuration = 0.25f;
        
        [SerializeField]
        private Ease fadeInEase = Ease.Linear;
        
        [SerializeField]
        private float fadeOutDuration = 0.3f;
        
        [SerializeField]
        private Ease fadeOutEase = Ease.Linear;
        
        private GameManager gameManager;
        private CanvasGroup canvasGroup;
        
        private int waveCount = 0; 
        private int rerollCount = 0;

        private void Awake()
        {
            GetGameManager().Forget();
        }

        private async UniTaskVoid GetGameManager()
        {
            gameManager = await GameManager.Get();
        }

        private void OnEnable()
        {
            Events.OnWaveEnded += OnWaveEnded;
            
            canvasGroup = upgradeCardParent.GetComponentInChildren<CanvasGroup>();
            for (int i = 0; i < upgradeCards.Length; i++)
            {
                upgradeCards[i].OnUpgradePicked += OnOnUpgradePicked;
            }
        }
        private void OnDisable()
        {
            Events.OnWaveEnded -= OnWaveEnded;
            
            for (int i = 0; i < upgradeCards.Length; i++)
            {
                upgradeCards[i].OnUpgradePicked -= OnOnUpgradePicked;
            }
        }

        private void OnOnUpgradePicked()
        {
            HideCards();
        }

        private void OnWaveEnded()
        {
            UIEvents.OnFocusChanged?.Invoke();

            waveCount++;
            rerollCount = 0;

            DisplayUpgradeCards();
        }
        
        public void DisplayUpgradeCards()
        {
            canvasGroup.alpha = 0;
            canvasGroup.interactable = true;
            canvasGroup.DOFade(1, fadeInDuration).SetEase(fadeInEase);
            
            int seed = gameManager.Seed + waveCount;
            List<UpgradeCardData> datas = upgradeDataUtility.GetRandomData(seed, upgradeCards.Length);

            for (int i = 0; i < upgradeCards.Length; i++)
            {
                upgradeCards[i].DisplayUpgrade(datas[i]);
            }

            upgradeCardParent.SetActive(true);
        }

        public void RerollUpgradeCards()
        {
            rerollCount++;
            int seed = gameManager.Seed + waveCount + rerollCount;
            List<UpgradeCardData> datas = upgradeDataUtility.GetRandomData(seed, upgradeCards.Length);

            for (int i = 0; i < upgradeCards.Length; i++)
            {
                upgradeCards[i].DisplayUpgrade(datas[i]);
            }
        }

        private void HideCards()
        {
            canvasGroup.DOKill();
            canvasGroup.interactable = false;
            
            canvasGroup.DOFade(0, fadeOutDuration).SetEase(fadeOutEase).onComplete = () =>
            {
                upgradeCardParent.SetActive(false);
            };
        }
    }
}
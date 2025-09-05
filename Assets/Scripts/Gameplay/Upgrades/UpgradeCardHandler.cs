using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using DG.Tweening;
using UnityEngine;

namespace Gameplay.Upgrades
{
    public class UpgradeCardHandler : MonoBehaviour
    {
        [Title("Settings")]
        [SerializeField]
        private int turnInterval = 10;
        
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
        
        private int rerollCount;
        private int lastCardTurn;

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
            upgradeDataUtility.InitializeUpgrades();
            upgradeDataUtility.StartObserving();

            Events.OnUpgradeCardPicked += OnUpgradePicked;
            Events.OnTurnIncreased += OnTurnIncreased;
            
            canvasGroup = upgradeCardParent.GetComponentInChildren<CanvasGroup>();
        }

        private void OnDisable()
        {
            upgradeDataUtility.StopObserving();
            
            Events.OnUpgradeCardPicked -= OnUpgradePicked;
            Events.OnTurnIncreased -= OnTurnIncreased;
        }

        private void OnTurnIncreased(int increase, int total)
        {
            int diff = total - lastCardTurn;
            if (diff % turnInterval != 0) return;
            
            lastCardTurn = total;
            DisplayUpgradeCards();
        }

        private void OnUpgradePicked(UpgradeCardData.UpgradeCardInstance _)
        {
            HideCards();
        }
        
        public void DisplayUpgradeCards()
        {
            UIEvents.OnFocusChanged?.Invoke();
            rerollCount = 0;

            canvasGroup.alpha = 0;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.DOFade(1, fadeInDuration).SetEase(fadeInEase);
            
            int seed = gameManager.Seed + lastCardTurn;
            List<UpgradeCardData.UpgradeCardInstance> datas = upgradeDataUtility.GetRandomData(seed, upgradeCards.Length);

            for (int i = 0; i < upgradeCards.Length; i++)
            {
                upgradeCards[i].DisplayUpgrade(datas[i]);
            }

            upgradeCardParent.SetActive(true);
        }

        public void RerollUpgradeCards()
        {
            rerollCount++;
            int seed = gameManager.Seed + lastCardTurn + rerollCount;
            List<UpgradeCardData.UpgradeCardInstance> datas = upgradeDataUtility.GetRandomData(seed, upgradeCards.Length);

            for (int i = 0; i < upgradeCards.Length; i++)
            {
                upgradeCards[i].DisplayUpgrade(datas[i]);
            }
        }

        private void HideCards()
        {
            canvasGroup.DOKill();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            
            canvasGroup.DOFade(0, fadeOutDuration).SetEase(fadeOutEase).onComplete = () =>
            {
                upgradeCardParent.SetActive(false);
            };
        }
    }
}
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
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

        private GameManager gameManager;
        
        private int waveCount = 0; 

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
            waveCount++;
            DisplayUpgradeCards();
        }
        
        public void DisplayUpgradeCards()
        {
            int seed = gameManager.Seed + waveCount;
            List<UpgradeCardData> datas = upgradeDataUtility.GetRandomData(seed, upgradeCards.Length);

            for (int i = 0; i < upgradeCards.Length; i++)
            {
                upgradeCards[i].DisplayUpgrade(datas[i]);
            }

            upgradeCardParent.SetActive(true);
        }

        private void HideCards()
        {
            upgradeCardParent.SetActive(false);
        }
    }
}
using System;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Upgrades
{
    public class UIUpgradeCard : MonoBehaviour
    {
        public event Action OnUpgradePicked;

        [Title("References")]
        [SerializeField]
        private Image iconImage;

        [SerializeField]
        private TextMeshProUGUI descriptionText;

        private UpgradeCardData upgradeCardData;

        public void DisplayUpgrade(UpgradeCardData upgradeData)
        {
            upgradeCardData = upgradeData;
            iconImage.sprite = upgradeData.Icon;
            descriptionText.text = upgradeData.Description;
        }

        public void PickUpgradeCard()
        {
            upgradeCardData.Perform();
            OnUpgradePicked?.Invoke();
        }
    }
}
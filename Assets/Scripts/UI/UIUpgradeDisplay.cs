using Effects;
using Sirenix.OdinInspector;
using Gameplay.Money;
using UnityEngine.UI;
using UnityEngine;

namespace UI
{
    public class UIUpgradeDisplay : PooledMonoBehaviour
    {
        [Title("References")]
        [SerializeField]
        private StupidButton button;

        [SerializeField]
        private Image iconImage;

        private IUpgradeStat upgradeStat;

        private bool hoveredLastFrame;

        public UIDistrictUpgrade DistrictUpgrade { get; set; }

        protected override void OnDisable()
        {
            base.OnDisable();

            MoneyManager.Instance.OnMoneyChanged -= UpdateTheButton;
        }

        private void Update()
        {
            if (!hoveredLastFrame && button.Hovered)
            {
                DistrictUpgrade.DisplayUpgrade(upgradeStat);
            }

            hoveredLastFrame = button.Hovered;
        }

        private void UpdateTheButton(float _)
        {
            UpdateButton();
        }

        public void DisplayStat(IUpgradeStat stat)
        {
            upgradeStat = stat;
            iconImage.sprite = upgradeStat.Icon;

            UpdateButton();

            MoneyManager.Instance.OnMoneyChanged += UpdateTheButton;
        }

        private void UpdateButton()
        {
            button.interactable = DistrictUpgrade.CanPurchase(upgradeStat);
        }

        public void ClickUpgrade()
        {
            if (DistrictUpgrade.CanPurchase(upgradeStat))
            {
                DistrictUpgrade.UpgradeStat(upgradeStat);
            }
        }
    }
}
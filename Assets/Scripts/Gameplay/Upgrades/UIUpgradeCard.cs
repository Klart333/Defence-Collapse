using Sirenix.OdinInspector;
using UnityEngine.UI;
using UnityEngine;
using DG.Tweening;
using TMPro;

namespace Gameplay.Upgrades
{
    public class UIUpgradeCard : MonoBehaviour
    {
        [Title("References")]
        [SerializeField]
        private Image iconImage;

        [SerializeField]
        private TextMeshProUGUI descriptionText;

        [Title("Rank")]
        [SerializeField]
        private Image rankImage;
        
        [SerializeField]
        private RankColorUtility colorUtility;
        
        [Title("Color")]
        [SerializeField]
        private float defaultColorAlpha = 0.07843137255f;
        
        [SerializeField]
        private float hoveredColorAlpha = 0.2352941176f;
        
        [Title("Animation")]
        [SerializeField]
        private float alphaAnimationDuration = 0.2f;
        
        [SerializeField]
        private Ease alphaAnimationEase = Ease.OutBack;
        
        private UpgradeCardData.UpgradeCardInstance upgradeCardInstance;

        public void DisplayUpgrade(UpgradeCardData.UpgradeCardInstance upgradeInstance)
        {
            upgradeCardInstance = upgradeInstance;
            iconImage.sprite = upgradeInstance.Icon;
            descriptionText.text = upgradeInstance.Description;

            Color color = colorUtility.GetColor(upgradeInstance.UpgradeRank);       
            color.a = defaultColorAlpha;
            rankImage.color = color;
        }

        public void OnPointerEnter()
        {
            rankImage.DOKill();
            rankImage.DOFade(hoveredColorAlpha, alphaAnimationDuration).SetEase(alphaAnimationEase);
        }

        public void OnPointerExit()
        {
            rankImage.DOKill();
            rankImage.DOFade(defaultColorAlpha, alphaAnimationDuration).SetEase(alphaAnimationEase);
        }

        public void PickUpgradeCard()
        {
            Events.OnUpgradeCardPicked?.Invoke(upgradeCardInstance);
        }
    }
}
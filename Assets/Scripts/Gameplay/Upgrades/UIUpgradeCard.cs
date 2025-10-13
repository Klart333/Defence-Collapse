using System.Collections.Generic;
using Sirenix.OdinInspector;
using Buildings.District.UI;
using Buildings.District;
using UnityEngine.UI;
using Gameplay.Event;
using UnityEngine;
using DG.Tweening;
using Variables;
using System;
using Effects;
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

        [Title("District Application")]
        [SerializeField]
        private Transform appliesContainer;
        
        [SerializeField]
        private Transform districtIconContainer;

        [SerializeField]
        private UIDistrictIcon districtIconPrefab;
        
        [SerializeField]
        private DistrictDataUtility districtDataUtility;
        
        [Title("Animation")]
        [SerializeField]
        private float alphaAnimationDuration = 0.2f;
        
        [SerializeField]
        private Ease alphaAnimationEase = Ease.OutBack;
        
        private Queue<UIDistrictIcon> disabledIcons = new Queue<UIDistrictIcon>();
        private Stack<UIDistrictIcon> spawnedIcons = new Stack<UIDistrictIcon>(); 
        
        private UpgradeCardData.UpgradeCardInstance upgradeCardInstance;

        public void DisplayUpgrade(UpgradeCardData.UpgradeCardInstance upgradeInstance)
        {
            upgradeCardInstance = upgradeInstance;
            iconImage.sprite = upgradeInstance.Icon;
            descriptionText.text = GetDescription(upgradeInstance.Description, upgradeInstance);

            Color color = colorUtility.GetColor(upgradeInstance.UpgradeRank);       
            color.a = defaultColorAlpha;
            rankImage.color = color;
            
            appliesContainer.gameObject.SetActive(upgradeInstance.IsAppliedToDistrct);
            if (upgradeInstance.IsAppliedToDistrct)
            {
                DisplayAppliedIcons(upgradeInstance.AppliedCategories);
            }
        }

        private void DisplayAppliedIcons(CategoryType categoryType)
        {
            DisableIcons();
            
            List<DistrictType> districtTypes = new List<DistrictType>();
            
            Array values = Enum.GetValues(typeof(CategoryType));
            foreach (object categoryTypeValue in values)
            {
                CategoryType value = (CategoryType)categoryTypeValue;
                if (categoryType.HasFlag(value) && CategoryTypeUtility.GetDistrictType(value, out var districtType))
                {
                    districtTypes.Add(districtType);
                }    
            }

            foreach (DistrictType districtType in districtTypes)
            {
                GetIcon().DisplayDistrict(districtDataUtility.GetTowerData(districtType), null);
            }
        }

        public UIDistrictIcon GetIcon()
        {
            if (disabledIcons.TryDequeue(out UIDistrictIcon icon))
            {
                icon.gameObject.SetActive(true);
                spawnedIcons.Push(icon);
                return icon;
            }

            icon = Instantiate(districtIconPrefab, districtIconContainer);
            spawnedIcons.Push(icon);
            return icon;
        }
        
        private void DisableIcons()
        {
            while (spawnedIcons.TryPop(out var value))
            {
                value.gameObject.SetActive(false);
                disabledIcons.Enqueue(value);
            }
        }

        private string GetDescription(StringReference descriptionReference, UpgradeCardData.UpgradeCardInstance upgrade)
        {
            if (descriptionReference.Mode == ReferenceMode.Constant)
            {
                return descriptionReference.Value;
            }

            if (descriptionReference.Variable is not { } stringVariable)
            {
                Debug.LogError("Description reference is not a string variable");
                return "";
            }

            if (upgrade.Effects == null || upgrade.Effects.Count == 0)
            {
                return stringVariable.LocalizedText.GetLocalizedString();
            }
            
            Dictionary<string, string> dict = new Dictionary<string, string>();
            for (int i = 0; i < upgrade.Effects.Count; i++)
            {
                dict.Add(i.ToString(), GetEffectString(upgrade.Effects[i]));
            }
            return stringVariable.LocalizedText.GetLocalizedString(dict);
        }
        
        public string GetEffectString(IEffect effect)
        {
            switch (effect)
            {
                case IncreaseStatEffect statEffect:
                    return statEffect.ModifierType switch
                    {
                        Modifier.ModifierType.Multiplicative => (statEffect.ModifierValue - 1.0f).ToString("P"),
                        Modifier.ModifierType.Additive => statEffect.ModifierValue.ToString("N1"),
                        _ => statEffect.ModifierValue.ToString("N0")
                    };
            }
            return "EFFECT NOT IMPLEMENTED";
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
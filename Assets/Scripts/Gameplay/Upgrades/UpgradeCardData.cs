using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Variables;
using Effects;
using System;

namespace Gameplay.Upgrades
{
    [InlineEditor, CreateAssetMenu(fileName = "Upgrade Card Data", menuName = "Upgrade/Upgrade Card Data", order = 0)]
    public class UpgradeCardData : SerializedScriptableObject
    {
        [Title("Description")]
        [SerializeField]
        private SpriteReference iconReference;

        [SerializeField]
        private StringReference descriptionReference;

        [Title("Weight Settings")]
        [SerializeField]
        private UpgradeRank upgradeRank;
        
        [SerializeField]
        private UpgradeCardType upgradeCardType;
        
        [SerializeField]
        private float weight = 1;

        [SerializeField]
        private WeightStrategy weightStrategy = WeightStrategy.DontChange;

        [SerializeField]
        [ShowIf(nameof(isChangeOnPicked))]
        private float weightChangeOnPicked = 1;
        
        [SerializeField]
        [ShowIf(nameof(isChangeOnCardPicked))]
        private float weightChangeOnCardsPicked = 1;

        [SerializeField]
        [ShowIf(nameof(isChangeOnDistrictPlaced))]
        private float weightChangeOnDistrictBuilt = 1;
        
        [Title("Effect")]
        [SerializeField]
        private CategoryType appliedCategories;
        
        [SerializeField]
        private UpgradeType upgradeType;
        
        [SerializeField, ShowIf(nameof(isEffectType))]
        private List<IEffect> effects = new List<IEffect>();
        
        [SerializeField, ShowIf(nameof(isComponent))]
        private UpgradeComponentType componentType;

        [SerializeField, ShowIf(nameof(isComponent))]
        private float componentStrength = 1;

        private bool isEffectType => upgradeType is UpgradeType.Effect or UpgradeType.StandAloneEffect;
        private bool isComponent => upgradeType == UpgradeType.Component;
        private bool isChangeOnPicked => weightStrategy.HasFlag(WeightStrategy.ChangeOnPicked);
        private bool isChangeOnCardPicked => weightStrategy.HasFlag(WeightStrategy.ChangeWithCardsPicked);
        private bool isChangeOnDistrictPlaced => weightStrategy.HasFlag(WeightStrategy.ChangeWithDistrictsBuilt);
        private bool isLockedToDistrictType => weightStrategy.HasFlag(WeightStrategy.LockToDistrictType);

        public UpgradeCardInstance GetUpgradeCardInstance() => new UpgradeCardInstance(this);

        public class UpgradeCardInstance
        {
            // ReSharper disable FieldCanBeMadeReadOnly.Global
            public List<IEffect> Effects;

            public StringReference Description;
            public Sprite Icon;

            public UpgradeComponentType ComponentType;
            public UpgradeCardType UpgradeCardType;
            public CategoryType AppliedCategories;
            public WeightStrategy WeightStrategy;
            public UpgradeRank UpgradeRank;
            public UpgradeType UpgradeType;

            public float WeightChangeOnDistrictBuilt;
            public float WeightChangeOnCardsPicked;
            public float WeightChangeOnPicked;
            public float ComponentStrength;
            public float Weight;

            public bool IsAppliedToDistrct => (AppliedCategories & CategoryType.AllDistrict) > 0;
            
            public UpgradeCardInstance(UpgradeCardData upgradeCardData)
            {
                Effects = upgradeCardData.effects;
                Icon = upgradeCardData.iconReference.Value;
                
                ComponentType = upgradeCardData.componentType;
                UpgradeCardType = upgradeCardData.upgradeCardType;
                AppliedCategories = upgradeCardData.appliedCategories;
                WeightStrategy = upgradeCardData.weightStrategy;
                UpgradeRank = upgradeCardData.upgradeRank;
                UpgradeType = upgradeCardData.upgradeType;
                
                WeightChangeOnDistrictBuilt = upgradeCardData.weightChangeOnDistrictBuilt;
                WeightChangeOnCardsPicked = upgradeCardData.weightChangeOnCardsPicked;
                WeightChangeOnPicked = upgradeCardData.weightChangeOnPicked;
                ComponentStrength = upgradeCardData.componentStrength;
                Description = upgradeCardData.descriptionReference;
                Weight = upgradeCardData.weight;
            }
        }
        
        #if UNITY_EDITOR
        [Button, TitleGroup("Debug")]
        private void DebugApplyUpgradeCard()
        {
            Events.OnUpgradeCardPicked?.Invoke(GetUpgradeCardInstance());
        }
        
        #endif
    }

    [Flags]
    public enum WeightStrategy
    {
        DontChange = 1 << 0,
        ChangeWithCardsPicked = 1 << 1,
        ChangeWithDistrictsBuilt = 1 << 2,
        ChangeOnPicked = 1 << 4,
        RemoveOnPicked = 1 << 5,
        LockToDistrictType = 1 << 6,
    }

    [Flags]
    public enum CategoryType
    {
        // District
        Archer = 1 << 0,
        Bomb = 1 << 1,
        Church = 1 << 2,
        TownHall = 1 << 3,
        Mine = 1 << 4,
        Flame = 1 << 5,
        Lightning = 1 << 10,
        Barracks = 1 << 11,
        Mana = 1 << 6,
        AllDistrict = Archer | Bomb | Church | TownHall | Mine | Flame | Lightning | Barracks | Mana,

        // Attack
        Projectile = 1 << 7,
        AoE = 1 << 8,
        AllAttacks = 1 << 9,
    }
    
    [Flags]
    public enum UpgradeCardType
    {
        // District
        Archer = 1 << 0,
        Bomb = 1 << 1,
        Church = 1 << 2,
        TownHall = 1 << 3,
        Mine = 1 << 4,
        Flame = 1 << 5,
        LightningDistrict = 1 << 10,
        Barracks = 1 << 14,
        AllDistrict = Archer | Bomb | Church | TownHall | Mine | Flame | LightningDistrict | Barracks,
        
        // Attack
        Projectile = 1 << 7,
        AoE = 1 << 8,
        AllAttacks = 1 << 9,
        
        // Standalone
        Fire = 1 << 11,
        Poison = 1 << 12,
        Lightning = 1 << 13,
    }

    public enum UpgradeType
    {
        Effect,
        Component,
        
        StandAloneEffect,
    }

    public enum UpgradeComponentType
    {
        Fire,
        Lightning,
        Explosion,
        MoneyOnDeath,
        Poison,
    }

    public enum UpgradeRank
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
    }

    public static class CategoryTypeUtility
    {
        public static bool GetDistrictType(CategoryType categoryType, out DistrictType districtType)
        {
             DistrictType? type = categoryType switch
            {
                CategoryType.Archer => DistrictType.Archer,
                CategoryType.Bomb => DistrictType.Bomb,
                CategoryType.Church => DistrictType.Church,
                CategoryType.TownHall => DistrictType.TownHall,
                CategoryType.Mine => DistrictType.Mine,
                CategoryType.Flame => DistrictType.Flame,
                CategoryType.Lightning => DistrictType.Lightning,
                CategoryType.Barracks => DistrictType.Barracks,
                _ => null
            };

            if (!type.HasValue)
            {
                districtType = default;
                return false;
            }
            
            districtType = type.Value;
            return true;
        }
    }
}
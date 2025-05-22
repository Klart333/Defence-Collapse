using Sirenix.OdinInspector;
using UnityEngine;
using Effects;
using System;
using System.Collections.Generic;

namespace Gameplay.Upgrades
{
    [CreateAssetMenu(fileName = "Upgrade Card Data", menuName = "Upgrade/Upgrade Card Data", order = 0)]
    public class UpgradeCardData : SerializedScriptableObject
    {
        public event Action<UpgradeCardData> OnUpgradePerformed;
        
        [Title("Description")]
        [SerializeField]
        private Sprite icon;
        
        [SerializeField, TextArea]
        private string description;

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

        private bool isEffectType => upgradeType == UpgradeType.Effect;
        private bool isComponent => upgradeType == UpgradeType.Component;
        
        public string Description => description;
        public Sprite Icon => icon;
        
        public CategoryType AppliedCategories => appliedCategories;
        public UpgradeComponentType ComponentType => componentType;
        public float ComponentStrength => componentStrength;
        public UpgradeType UpgradeType => upgradeType;
        public List<IEffect> Effects => effects;
        
        public void Perform()
        {
            OnUpgradePerformed?.Invoke(this);
        }
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
        AllDistrict = 1 << 6,
        
        // Attack
        Projectile = 1 << 7,
        AoE = 1 << 8,
        AllAttacks = 1 << 9,
    }

    public enum UpgradeType
    {
        Effect,
        Component,
    }

    public enum UpgradeComponentType
    {
        Fire,
        Lightning,
        Explosion,
        MoneyOnDeath,
    }
}
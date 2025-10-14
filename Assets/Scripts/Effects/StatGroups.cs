using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Effects
{
    public interface IStatGroup
    {
        public Stat[] GetStats();
    }

    // If you change you also gotta change in the utility...
    
    [Serializable]
    public class AttackDistrictStats : IStatGroup
    {
        [FoldoutGroup("Damage")]
        [SerializeField, InlineProperty, HideLabel]
        private AttackDamageStat AttackDamageStat = new AttackDamageStat(1);
        
        [FoldoutGroup("Damage")]
        [SerializeField, InlineProperty, HideLabel]
        private ArmorPenetrationStat ArmorPenetrationStat = new ArmorPenetrationStat(0);

        [FoldoutGroup("Crit")]
        [SerializeField, InlineProperty, HideLabel]
        private CritChanceStat CritChanceStat = new CritChanceStat(0.01f);
        
        [FoldoutGroup("Crit")]
        [SerializeField, InlineProperty, HideLabel]
        private CritDamageStat CritDamageStat = new CritDamageStat(2);
        
        [FoldoutGroup("Attacking")]
        [SerializeField, InlineProperty, HideLabel]
        private AttackSpeedStat AttackSpeedStat = new AttackSpeedStat(1);
        
        [FoldoutGroup("Attacking")]
        [SerializeField, InlineProperty, HideLabel]
        private RangeStat RangeStat = new RangeStat(6);

        public Stat[] GetStats()
        {
            return new Stat[]
            {
                new AttackDamageStat(AttackDamageStat),
                new ArmorPenetrationStat(ArmorPenetrationStat),
                new CritChanceStat(CritChanceStat),
                new CritDamageStat(CritDamageStat),
                new AttackSpeedStat(AttackSpeedStat),
                new RangeStat(RangeStat),
            };
        }
    }
    
    [Serializable]
    public class ProductionDistrictStats : IStatGroup
    {
        [FoldoutGroup("Production")]
        [SerializeField, InlineProperty, HideLabel]
        private ProductionSpeedStat ProductionSpeedStat = new ProductionSpeedStat(1);
        
        [FoldoutGroup("Production")]
        [SerializeField, InlineProperty, HideLabel]
        private GoldMultiplierStat GoldMultiplierStat = new GoldMultiplierStat(1);

        [FoldoutGroup("Production")]
        [SerializeField, InlineProperty, HideLabel]
        private BuffPowerStat BuffPowerStat = new BuffPowerStat(1);
        
        public Stat[] GetStats()
        {
            return new Stat[]
            {
                new ProductionSpeedStat(ProductionSpeedStat),
                new GoldMultiplierStat(GoldMultiplierStat),
                new BuffPowerStat(BuffPowerStat),
            };
        }
    }
    
    [Serializable]
    public class EffectsDistrictStats : IStatGroup
    {
        [FoldoutGroup("Damage Over Time")]
        [SerializeField, InlineProperty, HideLabel]
        private FireStat FireStat = new FireStat(0);
        
        [FoldoutGroup("Damage Over Time")]
        [SerializeField, InlineProperty, HideLabel]
        private PoisonStat PoisonStat = new PoisonStat(0);

        [FoldoutGroup("Damage Over Time")]
        [SerializeField, InlineProperty, HideLabel]
        private BleedStat BleedStat = new BleedStat(0);
        
        [FoldoutGroup("Crowd Control")]
        [SerializeField, InlineProperty, HideLabel]
        private SlowStat SlowStat = new SlowStat(0);

        
        public Stat[] GetStats()
        {
            return new Stat[]
            {
                new FireStat(FireStat),
                new PoisonStat(PoisonStat),
                new BleedStat(BleedStat),
                new SlowStat(SlowStat),
            };
        }
    }
    
    [Serializable]
    public class WallStats : IStatGroup
    {
        [FoldoutGroup("Health")]
        [SerializeField, InlineProperty, HideLabel]
        private MaxHealthStat MaxHealthStat = new MaxHealthStat(100);
        
        [FoldoutGroup("Health")]
        [SerializeField, InlineProperty, HideLabel]
        private HealingStat HealingStat = new HealingStat(0);

        public Stat[] GetStats()
        {
            return new Stat[]
            {
                new MaxHealthStat(MaxHealthStat),
                new HealingStat(HealingStat),
            };
        }
    }
    
    [Serializable]
    public class EnemyStats : IStatGroup
    {
        [FoldoutGroup("Attacking")]
        [SerializeField, InlineProperty, HideLabel]
        private AttackDamageStat AttackDamageStat = new AttackDamageStat(1);
        
        [FoldoutGroup("Attacking")]
        [SerializeField, InlineProperty, HideLabel]
        private AttackSpeedStat AttackSpeedStat = new AttackSpeedStat(1);
        
        [FoldoutGroup("Health")]
        [SerializeField, InlineProperty, HideLabel]
        private MaxHealthStat MaxHealthStat = new MaxHealthStat(100);

        [FoldoutGroup("Health")]
        [SerializeField, InlineProperty, HideLabel]
        private MaxArmorStat MaxArmorStat = new MaxArmorStat(10);
        
        [FoldoutGroup("Health")]
        [SerializeField, InlineProperty, HideLabel]
        private ArmorStat ArmorStat = new ArmorStat(0);
        
        [FoldoutGroup("Health")]
        [SerializeField, InlineProperty, HideLabel]
        private HealingStat HealingStat = new HealingStat(0);
        
        [FoldoutGroup("Movement")]
        [SerializeField, InlineProperty, HideLabel]
        private MovementSpeedStat MovementSpeedStat = new MovementSpeedStat(0.5f);

        public Stat[] GetStats()
        {
            return new Stat[]
            {
                new AttackDamageStat(AttackDamageStat),
                new MaxHealthStat(MaxHealthStat),
                new MaxArmorStat(MaxArmorStat),
                new HealingStat(HealingStat),
                new AttackSpeedStat(AttackSpeedStat),
                new MovementSpeedStat(MovementSpeedStat),
                new ArmorStat(ArmorStat),
            };
        }
    }

}
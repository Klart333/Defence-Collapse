using System;

namespace Effects
{
    #region District Attack Stats

    [Serializable]
    public class AttackDamageStat : Stat
    {
        public AttackDamageStat(float baseValue) : base(baseValue) { }
        public AttackDamageStat(Stat copyFrom) : base(copyFrom) { }
    }
    
    [Serializable]
    public class ArmorPenetrationStat : Stat, IPercentageStat
    {
        public ArmorPenetrationStat(float baseValue) : base(baseValue) { }
        public ArmorPenetrationStat(Stat copyFrom) : base(copyFrom) { }
    }
    
    [Serializable]
    public class CritChanceStat : Stat, IPercentageStat
    {
        public CritChanceStat(float baseValue) : base(baseValue) { }
        public CritChanceStat(Stat copyFrom) : base(copyFrom) { }
    }
    
    [Serializable]
    public class CritDamageStat : Stat
    {
        public CritDamageStat(float baseValue) : base(baseValue) { }
        public CritDamageStat(Stat copyFrom) : base(copyFrom) { }
    }
    
    [Serializable]
    public class AttackSpeedStat : Stat
    {
        public AttackSpeedStat(float baseValue) : base(baseValue) { }
        public AttackSpeedStat(Stat copyFrom) : base(copyFrom) { }
    }
    
    [Serializable]
    public class RangeStat : Stat
    {
        public RangeStat(float baseValue) : base(baseValue) { }
        public RangeStat(Stat copyFrom) : base(copyFrom) { }
    }

    #endregion

    #region Production District Stats

    [Serializable]
    public class GoldMultiplierStat : Stat
    {
        public GoldMultiplierStat(float baseValue) : base(baseValue) { }
        public GoldMultiplierStat(Stat copyFrom) : base(copyFrom) { }
    }
    
    [Serializable]
    public class ProductionSpeedStat : Stat
    {
        public ProductionSpeedStat(float baseValue) : base(baseValue) { }
        public ProductionSpeedStat(Stat copyFrom) : base(copyFrom) { }
    }
    
    [Serializable]
    public class BuffPowerStat : Stat
    {
        public BuffPowerStat(float baseValue) : base(baseValue) { }
        public BuffPowerStat(Stat copyFrom) : base(copyFrom) { }
    }

    #endregion
    
    #region Effect District Stats

    [Serializable]
    public class FireStat : Stat
    {
        public FireStat(float baseValue) : base(baseValue) { }
        public FireStat(Stat copyFrom) : base(copyFrom) { }
    }
    
    [Serializable]
    public class PoisonStat : Stat
    {
        public PoisonStat(float baseValue) : base(baseValue) { }
        public PoisonStat(Stat copyFrom) : base(copyFrom) { }
    }
    
    [Serializable]
    public class BleedStat : Stat
    {
        public BleedStat(float baseValue) : base(baseValue) { }
        public BleedStat(Stat copyFrom) : base(copyFrom) { }
    }
    
    [Serializable]
    public class SlowStat : Stat
    {
        public SlowStat(float baseValue) : base(baseValue) { }
        public SlowStat(Stat copyFrom) : base(copyFrom) { }
    }

    #endregion
    
    #region Wall Stats

    [Serializable]
    public class MaxHealthStat : Stat
    {
        public MaxHealthStat(float baseValue) : base(baseValue) { }
        public MaxHealthStat(Stat copyFrom) : base(copyFrom) { }
    }
    
    [Serializable]
    public class HealingStat : Stat
    {
        public HealingStat(float baseValue) : base(baseValue) { }
        public HealingStat(Stat copyFrom) : base(copyFrom) { }
    }

    #endregion
    
    #region Enemy Stats

    [Serializable]
    public class MaxArmorStat : Stat
    {
        public MaxArmorStat(float baseValue) : base(baseValue) { }
        public MaxArmorStat(Stat copyFrom) : base(copyFrom) { }
    }
    
    [Serializable]
    public class ArmorStat : Stat
    {
        public ArmorStat(float baseValue) : base(baseValue) { }
        public ArmorStat(Stat copyFrom) : base(copyFrom) { }
    }

    
    [Serializable]
    public class MovementSpeedStat : Stat
    {
        public MovementSpeedStat(float baseValue) : base(baseValue) { }
        public MovementSpeedStat(Stat copyFrom) : base(copyFrom) { }
    }

    #endregion
}
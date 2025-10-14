using System;

namespace Effects
{
    #region District Attack Stats

    [Serializable]
    public class AttackDamageStat : Stat
    {
        public AttackDamageStat(float baseValue) : base(baseValue)
        {
            
        }
    }
    
    [Serializable]
    public class ArmorPenetrationStat : Stat
    {
        public ArmorPenetrationStat(float baseValue) : base(baseValue)
        {
            
        }
    }
    
    [Serializable]
    public class CritChanceStat : Stat
    {
        public CritChanceStat(float baseValue) : base(baseValue)
        {
            
        }
    }
    
    [Serializable]
    public class CritDamageStat : Stat
    {
        public CritDamageStat(float baseValue) : base(baseValue)
        {
            
        }
    }
    
    [Serializable]
    public class AttackSpeedStat : Stat
    {
        public AttackSpeedStat(float baseValue) : base(baseValue)
        {
            
        }
    }
    
    [Serializable]
    public class RangeStat : Stat
    {
        public RangeStat(float baseValue) : base(baseValue)
        {
            
        }
    }

    #endregion

    #region Production District Stats

    [Serializable]
    public class GoldMultiplierStat : Stat
    {
        public GoldMultiplierStat(float baseValue) : base(baseValue)
        {
            
        }
    }
    
    [Serializable]
    public class ProductionSpeedStat : Stat
    {
        public ProductionSpeedStat(float baseValue) : base(baseValue)
        {
            
        }
    }
    
    [Serializable]
    public class BuffPowerStat : Stat
    {
        public BuffPowerStat(float baseValue) : base(baseValue)
        {
            
        }
    }

    #endregion
    
    #region Effect District Stats

    [Serializable]
    public class FireStat : Stat
    {
        public FireStat(float baseValue) : base(baseValue)
        {
            
        }
    }
    
    [Serializable]
    public class PoisonStat : Stat
    {
        public PoisonStat(float baseValue) : base(baseValue)
        {
            
        }
    }
    
    [Serializable]
    public class BleedStat : Stat
    {
        public BleedStat(float baseValue) : base(baseValue)
        {
            
        }
    }
    
    [Serializable]
    public class SlowStat : Stat
    {
        public SlowStat(float baseValue) : base(baseValue)
        {
            
        }
    }

    #endregion
    
    #region Wall Stats

    [Serializable]
    public class MaxHealthStat : Stat
    {
        public MaxHealthStat(float baseValue) : base(baseValue)
        {
            
        }
    }
    
    [Serializable]
    public class HealingStat : Stat
    {
        public HealingStat(float baseValue) : base(baseValue)
        {
            
        }
    }

    #endregion
    
    #region Enemy Stats

    [Serializable]
    public class MaxArmorStat : Stat
    {
        public MaxArmorStat(float baseValue) : base(baseValue)
        {
            
        }
    }
    
    [Serializable]
    public class ArmorStat : Stat
    {
        public ArmorStat(float baseValue) : base(baseValue)
        {
            
        }
    }

    
    [Serializable]
    public class MovementSpeedStat : Stat
    {
        public MovementSpeedStat(float baseValue) : base(baseValue)
        {
            
        }
    }

    #endregion
}
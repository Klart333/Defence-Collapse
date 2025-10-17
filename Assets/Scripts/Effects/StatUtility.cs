using System;
using System.Collections.Generic;
using UnityEngine;

namespace Effects
{
    public static class StatUtility
    {
        public static readonly Dictionary<Type, Type[]> StatTypes = new Dictionary<Type, Type[]>
        {
            {
                typeof(AttackDistrictStats), 
                new Type[]
                {
                    typeof(AttackDamageStat),
                    typeof(ArmorPenetrationStat),
                    typeof(CritChanceStat),
                    typeof(CritDamageStat),
                    typeof(AttackSpeedStat),
                    typeof(RangeStat),
                }
            },
            {
                typeof(ProductionDistrictStats),
                new Type[]
                {
                    typeof(GoldMultiplierStat),
                    typeof(ProductionSpeedStat),
                    typeof(BuffPowerStat),
                }
            },
            {
                typeof(EffectsDistrictStats),
                new Type[]
                {
                    typeof(FireStat),
                    typeof(PoisonStat),
                    typeof(BleedStat),
                    typeof(SlowStat),
                }
            },
            {
                typeof(WallStats),
                new Type[]
                {
                    typeof(MaxHealthStat),
                    typeof(HealingStat),
                }
            },
            {
                typeof(EnemyStats), 
                new Type[]
                {
                    typeof(AttackDamageStat),
                    typeof(AttackSpeedStat),
                    typeof(MaxHealthStat),
                    typeof(MaxArmorStat),
                    typeof(ArmorStat),
                    typeof(HealingStat),
                    typeof(MovementSpeedStat),
                }
            },
        };

        public static List<Type> GetStatTypes(params Type[] groups)
        {
            List<Type> types = new List<Type>();
            foreach (Type group in groups)
            {
                if (!StatTypes.TryGetValue(group, out Type[] statTypes))
                {
                    Debug.LogError($"Could not find Stat Group of type: {group}");
                    continue;
                }
                
                types.AddRange(statTypes);
            }
            
            return types;
        }
    }
}
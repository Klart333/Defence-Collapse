using System.Collections.Generic;
using UnityEngine;
using System;

namespace Effects
{
    public class Stats
    {
        public readonly Dictionary<Type, Stat> StatDictionary = new Dictionary<Type, Stat>();

        public Stats(params IStatGroup[] groups)
        {
            foreach (IStatGroup group in groups)
            {
                foreach (Stat stat in group.GetStats())
                {
                    StatDictionary.Add(stat.GetType(), stat);
                }
            }
        }

        public Stat Get<T>() where T : Stat
        {
            return StatDictionary[typeof(T)];
        }
        
        public Stat Get(Type type)
        {
            return StatDictionary[type];
        }
        
        public void ModifyStat(Type statType, Modifier modifier)
        {
            if (!StatDictionary.TryGetValue(statType, out Stat stat))
            {
                Debug.LogError($"Trying to modify stat that does not exist, type = {statType}");
                return;
            }
            stat.AddModifier(modifier);
        }

        public void RevertModifiedStat(Type statType, Modifier modifier)
        {
            if (!StatDictionary.TryGetValue(statType, out Stat stat))
            {
                Debug.LogError($"Trying to modify stat that does not exist, type = {statType}");
                return;
            }
            
            stat.RemoveModifier(modifier);
        }
    }
}
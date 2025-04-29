using Sirenix.OdinInspector;
using System;

[Serializable]
public class Stats
{
    [Title("Attacking")]
    public IStat DamageMultiplier = new Stat(1);
    public IStat AttackSpeed = new Stat(1);
    public IStat Range = new Stat(1);
    
    [Title("Movement")]
    public IStat MovementSpeed = new Stat(1);

    [Title("Crit")]
    public IStat CritChance = new Stat(0);
    public IStat CritMultiplier = new Stat(2);

    [Title("Defense")]
    public IStat Armor = new Stat(0);
    public IStat MaxHealth = new Stat(10);
    public IStat Healing = new Stat(0);

    public Stats()
    {
        
    }

    public Stats(Stats copy)
    {
        DamageMultiplier = new Stat(copy.DamageMultiplier.BaseValue);
        AttackSpeed = new Stat(copy.AttackSpeed.BaseValue);
        Range = new Stat(copy.Range.BaseValue);
        
        MovementSpeed = new Stat(copy.MovementSpeed.BaseValue);
        
        CritChance = new Stat(copy.CritChance.BaseValue);
        CritMultiplier = new Stat(copy.CritMultiplier.BaseValue);
        
        Armor = new Stat(copy.Armor.BaseValue);
        MaxHealth = new Stat(copy.MaxHealth.BaseValue);
        Healing = new Stat(copy.Healing.BaseValue);
    }

    public IStat Get(StatType statType)
    {
        return statType switch
        {
            StatType.DamageMultiplier => DamageMultiplier,
            StatType.AttackSpeed => AttackSpeed,
            StatType.Range => Range,
            StatType.MovementSpeed => MovementSpeed,
            StatType.CritChance => CritChance,
            StatType.CritMultiplier => CritMultiplier,
            StatType.Armor => Armor,
            StatType.MaxHealth => MaxHealth,
            StatType.Healing => Healing,
            _ => null
        };
    }

    public void ModifyStat(StatType statType, Modifier modifier)
    {
        Get(statType).AddModifier(modifier);
    }

    public void RevertModifiedStat(StatType statType, Modifier modifier)
    {
        Get(statType).RemoveModifier(modifier);
    }

    public float GetCritMultiplier()
    {
        if (CritChance.Value <= 0)
        {
            return 1;
        }

        if (UnityEngine.Random.value < CritChance.Value)
        {
            return CritMultiplier.Value;
        }

        return 1;
    }
}

public enum StatType
{
    DamageMultiplier,
    AttackSpeed,
    Range,
    
    MovementSpeed,
    
    CritChance,
    CritMultiplier,
    
    Armor,
    MaxHealth,
    Healing,
}
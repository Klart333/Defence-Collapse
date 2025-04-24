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
        DamageMultiplier = new Stat(copy.DamageMultiplier.Value);
        AttackSpeed = new Stat(copy.AttackSpeed.Value);
        Range = new Stat(copy.Range.Value);
        
        MovementSpeed = new Stat(copy.MovementSpeed.Value);
        
        CritChance = new Stat(copy.CritChance.Value);
        CritMultiplier = new Stat(copy.CritMultiplier.Value);
        
        Armor = new Stat(copy.Armor.Value);
        MaxHealth = new Stat(copy.MaxHealth.Value);
        Healing = new Stat(copy.Healing.Value);
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

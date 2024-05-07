using Sirenix.OdinInspector;
using System;

[Serializable]
public class Stats
{
    [Title("Damage")]
    public Stat DamageMultiplier;

    [Title("Attack Speed")]
    public Stat AttackSpeed;
    public Stat MovementSpeed;

    [Title("Crit")]
    public Stat CritChance;
    public Stat CritMultiplier;

    [Title("Defense")]
    public Stat Armor;
    public Stat MaxHealth;

    [Title("Healing")]
    public Stat Healing;

    public Stats()
    {
        
    }

    public Stats(Stats copy)
    {
        AttackSpeed = new Stat(copy.AttackSpeed.Value);
        MaxHealth = new Stat(copy.MaxHealth.Value);
        DamageMultiplier = new Stat(copy.DamageMultiplier.Value);
        Healing = new Stat(copy.Healing.Value);
        Armor = new Stat(copy.Armor.Value);
        CritChance = new Stat(copy.CritChance.Value);
        CritMultiplier = new Stat(copy.CritMultiplier.Value);
        MovementSpeed = new Stat(copy.MovementSpeed.Value);
    }

    public Stat Get(StatType statType)
    {
        switch (statType)
        {
            case StatType.DamageMultiplier:
                return DamageMultiplier;
            case StatType.AttackSpeed:
                return AttackSpeed;
            case StatType.MovementSpeed:
                return MovementSpeed;
            case StatType.CritChance:
                return CritChance;
            case StatType.CritMultiplier:
                return CritMultiplier;
            case StatType.Armor:
                return Armor;
            case StatType.MaxHealth:
                return MaxHealth;
            case StatType.Healing:
                return Healing;
            default:
                return null;
        }
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
    DamageMultiplier = 10,

    AttackSpeed = 11,
    MovementSpeed = 12,
    
    CritChance = 13,
    CritMultiplier = 14,
    
    Armor = 15,
    MaxHealth = 16,
    
    Healing = 17,
}

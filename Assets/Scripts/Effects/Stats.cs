using Sirenix.OdinInspector;
using System;

[Serializable]
public class Stats
{
    [Title("Fire")]
    public Stat DamageMultiplier;

    [Title("Wind")]
    public Stat AttackSpeed;
    public Stat MovementSpeed;

    [Title("Thunder")]
    public Stat CritChance;
    public Stat CritMultiplier;

    [Title("Rock")]
    public Stat Armor;
    public Stat MaxHealth;

    [Title("Nature")]
    public Stat Healing;
    
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

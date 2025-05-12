using Sirenix.OdinInspector;
using System;

[Serializable]
public class Stats
{
    [FoldoutGroup("Damage")]
    public Stat HealthDamage = new Stat(1);
    [FoldoutGroup("Damage")]
    public Stat ArmorDamage = new Stat(1);
    [FoldoutGroup("Damage")]
    public Stat ShieldDamage = new Stat(1);
    
    [FoldoutGroup("Attacking")]
    public Stat AttackSpeed = new Stat(1);
    [FoldoutGroup("Attacking")]
    public Stat Range = new Stat(1);
    
    [FoldoutGroup("Movement")]
    public Stat MovementSpeed = new Stat(1);

    [FoldoutGroup("Crit")]
    public Stat CritChance = new Stat(0);
    [FoldoutGroup("Crit")]
    public Stat CritMultiplier = new Stat(2);

    [FoldoutGroup("Defense")]
    public Stat MaxHealth = new Stat(10);
    [FoldoutGroup("Defense")]
    public Stat MaxArmor = new Stat(0);
    [FoldoutGroup("Defense")]
    public Stat MaxShield = new Stat(0);
    [FoldoutGroup("Defense")]
    public Stat Healing = new Stat(0);

    [FoldoutGroup("Non-Combat")]
    public Stat Productivity = new Stat(0);

    public Stats()
    {
        
    }

    public Stats(Stats copy)
    {
        HealthDamage = new Stat(copy.HealthDamage.BaseValue);
        ArmorDamage = new Stat(copy.ArmorDamage.BaseValue);
        ShieldDamage = new Stat(copy.ShieldDamage.BaseValue);
        
        AttackSpeed = new Stat(copy.AttackSpeed.BaseValue);
        Range = new Stat(copy.Range.BaseValue);
        
        MovementSpeed = new Stat(copy.MovementSpeed.BaseValue);
        
        CritChance = new Stat(copy.CritChance.BaseValue);
        CritMultiplier = new Stat(copy.CritMultiplier.BaseValue);
        
        MaxArmor = new Stat(copy.MaxArmor.BaseValue);
        MaxShield = new Stat(copy.MaxShield.BaseValue);
        MaxHealth = new Stat(copy.MaxHealth.BaseValue);
        Healing = new Stat(copy.Healing.BaseValue);
        
        Productivity = new Stat(copy.Productivity.BaseValue);
    }

    public Stat Get(StatType statType)
    {
        return statType switch
        {
            StatType.HealthDamage => HealthDamage,
            StatType.ArmorDamage => ArmorDamage,
            StatType.ShieldDamage => ShieldDamage,
            
            StatType.AttackSpeed => AttackSpeed,
            StatType.Range => Range,
            
            StatType.MovementSpeed => MovementSpeed,
            
            StatType.CritChance => CritChance,
            StatType.CritMultiplier => CritMultiplier,
            
            StatType.MaxArmor => MaxArmor,
            StatType.MaxShield => MaxShield,
            StatType.MaxHealth => MaxHealth,
            StatType.Healing => Healing,
            
            StatType.Productivity => Productivity,
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
    HealthDamage,
    ArmorDamage,
    ShieldDamage,
    
    AttackSpeed,
    Range,
    
    MovementSpeed,
    
    CritChance,
    CritMultiplier,
    
    MaxArmor,
    MaxShield,
    MaxHealth,
    Healing,
    
    Productivity,
}
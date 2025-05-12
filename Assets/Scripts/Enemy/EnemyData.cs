using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;

[InlineEditor, CreateAssetMenu(fileName = "New Data", menuName = "Enemy/Attack Data")]
public class EnemyData : SerializedScriptableObject
{
    [TitleGroup("Stats")]
    [OdinSerialize, NonSerialized]
    public Stats Stats;

    [Title("Attack")]
    [OdinSerialize, NonSerialized]
    public Attack BaseAttack;

    [Title("Loot")]
    public List<float> LootProbability;

    [Title("Spawning")]
    public int UnlockedThreshold = 0;
    
    public int CreditCost = 1;

    [Title("OnDeath")]
    [SerializeField]
    private float moneyOnDeath = 5;
    
    [SerializeField]
    private bool explodeOnDeath = false;

    [ShowIf(nameof(explodeOnDeath))]
    [SerializeField]
    private float explosionSize = 0.5f;
    
    
    public bool ExplodeOnDeath => explodeOnDeath;
    public float ExplosionSize => explosionSize;
    public float MoneyOnDeath => moneyOnDeath;

    [TitleGroup("Stats")]
    [Button]
    public void InitStats()
    {
        Stats = new Stats
        {
            HealthDamage = Stats.HealthDamage != null ? new Stat(Stats.HealthDamage.Value) : new Stat(1),
            ArmorDamage = Stats.ArmorDamage != null ? new Stat(Stats.ArmorDamage.Value) : new Stat(1),
            ShieldDamage = Stats.ShieldDamage != null ? new Stat(Stats.ShieldDamage.Value) : new Stat(1),
            
            AttackSpeed = Stats.AttackSpeed != null ? new Stat(Stats.AttackSpeed.Value) : new Stat(1),
            Range = Stats.Range != null ? new Stat(Stats.Range.Value) : new Stat(1),
            
            MovementSpeed = Stats.MovementSpeed != null ? new Stat(Stats.MovementSpeed.Value) : new Stat(1),
            
            CritChance = Stats.CritChance != null ? new Stat(Stats.CritChance.Value) : new Stat(1),
            CritMultiplier = Stats.CritMultiplier != null ? new Stat(Stats.CritMultiplier.Value) : new Stat(1),
            
            MaxHealth = Stats.MaxHealth != null ? new Stat(Stats.MaxHealth.Value) : new Stat(1),
            MaxArmor = Stats.MaxArmor != null ? new Stat(Stats.MaxArmor.Value) : new Stat(1),
            MaxShield = Stats.MaxShield != null ? new Stat(Stats.MaxShield.Value) : new Stat(1),
            Healing = Stats.Healing != null ? new Stat(Stats.Healing.Value) : new Stat(1),
            
            Productivity = Stats.Productivity != null ? new Stat(Stats.Productivity.Value) : new Stat(1),
        };
    }
}

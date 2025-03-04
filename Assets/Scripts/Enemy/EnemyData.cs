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
    
    [TitleGroup("Stats")]
    [Button]
    public void InitStats()
    {
        Stats = new Stats
        {
            AttackSpeed = new Stat(1),
            MaxHealth = new Stat(1),
            DamageMultiplier = new Stat(1),
            MovementSpeed = new Stat(1),
            Healing = new Stat(0),
            Armor = new Stat(0),
            CritChance = new Stat(0),
            CritMultiplier = new Stat(0),
        };
    }
}

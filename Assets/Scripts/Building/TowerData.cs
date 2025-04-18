﻿using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

[InlineEditor, CreateAssetMenu(fileName = "New Tower Data", menuName = "Building/Tower Data")]
public class TowerData : SerializedScriptableObject
{
    [TitleGroup("Stats")]
    [OdinSerialize, NonSerialized]
    public Stats Stats;

    [Title("Range")]
    public int Range = 8;

    public PooledMonoBehaviour RangeIndicator;

    [Title("Attack")]
    [OdinSerialize, NonSerialized]
    public Attack BaseAttack;

    [TitleGroup("Stats")]
    [Button]
    public void InitStats()
    {
        Stats = new Stats
        {
            AttackSpeed = new Stat(1),
            MaxHealth = new Stat(10),
            DamageMultiplier = new Stat(1),
            MovementSpeed = new Stat(0),
            Healing = new Stat(0),
            Armor = new Stat(0),
            CritChance = new Stat(1),
            CritMultiplier = new Stat(2),
        };
    }
}


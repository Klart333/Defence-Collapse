using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

[InlineEditor, CreateAssetMenu(fileName = "New Archer Data", menuName = "Building/State Data/Archer")]
public class ArcherData : SerializedScriptableObject
{
    [Title("Economy")]
    public int IncomePerHouse = 2;

    [Title("Stats")]
    public Stats Stats;

    [Title("Range")]
    public int Range = 8;

    public PooledMonoBehaviour RangeIndicator;

    [Title("Growth")]
    public float LevelMultiplier = 1;

    [Title("Attack")]
    [OdinSerialize, NonSerialized]
    public Attack BaseAttack;

    public LayerMask AttackLayerMask;

    private void OnValidate()
    {
        if (Stats == null)
        {
            Stats = new Stats
            {
                AttackSpeed = new Stat(1),
                MaxHealth = new Stat(20),
                DamageMultiplier = new Stat(1),
                MovementSpeed = new Stat(0),
                Healing = new Stat(0),
                Armor = new Stat(0),
                CritChance = new Stat(0),
                CritMultiplier = new Stat(0),
            };
        }
    }
}


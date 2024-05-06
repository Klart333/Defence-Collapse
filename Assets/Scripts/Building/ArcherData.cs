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
    public float AttackSpeed = 1;
    public float Range = 5;
    public float Damage = 1;

    [Title("Prefabs")]
    public PooledMonoBehaviour RangeIndicator;

    [Title("Growth")]
    public float LevelMultiplier = 1;

    [Title("Health")]
    public int MaxHealth;

    [Title("Attack")]
    [OdinSerialize, NonSerialized]
    public Attack BaseAttack;

    public LayerMask AttackLayerMask;
}


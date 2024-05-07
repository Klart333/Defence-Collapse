using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

[InlineEditor, CreateAssetMenu(fileName = "New Data", menuName = "Enemy/Attack Data")]
public class EnemyAttackData : SerializedScriptableObject
{
    [Title("Stats")]
    public Stats Stats;

    [Title("Hit Info")]
    public LayerMask LayerMask;

    public float AttackRadius;

    [Title("Attack")]
    [OdinSerialize, NonSerialized]
    public Attack BaseAttack;

    private void OnValidate()
    {
        if (Stats == null)
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
}

using System;
using UnityEngine;

public interface IAttacker
{
    // References
    public Stats Stats { get; }
    public Health Health { get; }   

    // Position
    public Vector3 AttackPosition { get; set; }
    public Vector3 OriginPosition { get; }

    // Attack
    public DamageInstance LastDamageDone {  get; }
    public LayerMask LayerMask { get; }

    // Events
    public event Action OnAttack;

    // Callbacks
    public void OnUnitDoneDamage(DamageInstance damageInstance);
    public void OnUnitKill();
}

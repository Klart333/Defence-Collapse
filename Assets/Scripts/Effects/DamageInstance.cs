using System.Collections.Generic;
using UnityEngine;

public class DamageInstance
{
    public IAttacker Source;
    public IHealth TargetHit;

    public float Damage;
    public float CritMultiplier;
    public Vector3 AttackPosition;

    private HashSet<int> specialEffectSet;

    public DamageInstance() { }

    public DamageInstance(DamageInstance damage)
    {
        this.Source = damage.Source;
        this.TargetHit = damage.TargetHit;
        this.Damage = damage.Damage;
        this.CritMultiplier = damage.CritMultiplier;
        this.AttackPosition = damage.AttackPosition;
        this.SpecialEffectSet = new HashSet<int>(damage.SpecialEffectSet);
    }

    public HashSet<int> SpecialEffectSet
    {
        get
        {
            specialEffectSet ??= new HashSet<int>();

            return specialEffectSet;
        }
        set
        {
            specialEffectSet = value;
        }
    }

    public float GetTotal()
    {
        return Damage;
    }
}

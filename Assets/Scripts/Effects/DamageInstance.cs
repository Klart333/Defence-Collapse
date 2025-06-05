using System.Collections.Generic;
using UnityEngine;

public class DamageInstance
{
    public IAttacker Source;

    public float Damage;
    public float CritMultiplier;
    public Vector3 AttackPosition;

    private HashSet<int> specialEffectSet;

    public DamageInstance() { }

    public DamageInstance(DamageInstance damage)
    {
        Source = damage.Source;
        Damage = damage.Damage;
        CritMultiplier = damage.CritMultiplier;
        AttackPosition = damage.AttackPosition;
        SpecialEffectSet = new HashSet<int>(damage.SpecialEffectSet);
    }
    
    public DamageInstance(float damage)
    {
        Damage = damage;
        CritMultiplier = 2;
        
        AttackPosition = default;
        SpecialEffectSet = null;
        Source = null;
    }

    public HashSet<int> SpecialEffectSet
    {
        get
        {
            specialEffectSet ??= new HashSet<int>();

            return specialEffectSet;
        }
        set => specialEffectSet = value;
    }

    public float GetTotal()
    {
        return Damage;
    }
}

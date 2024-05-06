using System.Collections.Generic;

public class DamageInstance
{
    public IAttacker Source;
    public IHealth TargetHit;

    public float Damage;
    public float CritMultiplier;

    private HashSet<int> specialEffectSet;

    public HashSet<int> SpecialEffectSet
    {
        get
        {
            if (specialEffectSet == null)
            {
                specialEffectSet = new HashSet<int>();
            }

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

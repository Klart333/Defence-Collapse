using Effects;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;

[System.Serializable]
public class Attack
{
    [OdinSerialize, NonSerialized]
    public List<IEffect> Effects = new List<IEffect>();

    public Attack(Attack copy)
    {
        Effects = new List<IEffect>(copy.Effects);
    }

    public void TriggerAttack(IAttacker attacker)
    {
        for (int i = 0; i < Effects.Count; i++)
        {
            Effects[i].Perform(attacker);
        }
    }
}
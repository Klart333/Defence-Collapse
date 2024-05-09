using Effects;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;

[System.Serializable]
public class Attack
{
    private List<IEffectHolder> effectHolders = new List<IEffectHolder>();

    public List<IEffectHolder> EffectHolders
    {
        get
        {
            if (effectHolders == null)
            {
                effectHolders = new List<IEffectHolder>();
            }

            return effectHolders;
        }
    }

    [OdinSerialize, NonSerialized]
    public List<IEffect> Effects = new List<IEffect>();

    public Attack(Attack copy)
    {
        effectHolders = new List<IEffectHolder>(copy.EffectHolders);
        Effects = new List<IEffect>(copy.Effects);
    }

    public void TriggerAttack(IAttacker attacker)
    {
        if (EffectHolders.Count == 0)
        {
            for (int i = 0; i < Effects.Count; i++)
            {
                Effects[i].Perform(attacker);
            }

            return;
        }

        EffectHolders[EffectHolders.Count - 1].Perform(attacker);
    }

    public void AddEffect(IEffect effect)
    {
        if (effect is IEffectHolder holder)
        {
            EffectHolders.Add(holder);
        }
        else
        {
            Effects.Add(effect);
        }

        BuildEffectHolder();
    }

    private void BuildEffectHolder()
    {
        for (int i = 0; i < EffectHolders.Count; i++)
        {
            if (i == 0)
            {
                EffectHolders[i].Effects = Effects;
            }
            else
            {
                EffectHolders[i].Effects = new List<IEffect>() { EffectHolders[i - 1] };
            }
        }
    }
}
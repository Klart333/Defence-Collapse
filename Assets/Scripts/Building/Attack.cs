using Effects;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Attack
{
    [OdinSerialize, NonSerialized]
    public List<IEffect> Effects = new List<IEffect>();

    [OdinSerialize, NonSerialized]
    public List<IEffect> DoneDamageEffects = new List<IEffect>();

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

    public void OnDoneDamage(IAttacker attacker)
    {
        for (int i = 0; i < DoneDamageEffects.Count; i++)
        {
            DoneDamageEffects[i].Perform(attacker);
        }
    }

    public void AddEffect(List<IEffect> effects, EffectType effectType)
    {
        switch (effectType)
        {
            case EffectType.Effect:
                Effects.AddRange(effects);
                break;
            case EffectType.Holder:
                for (int i = 0; i < effects.Count; i++)
                {
                    if (effects[i] is IEffectHolder holder)
                    {
                        effectHolders.Add(holder);
                    }
                    else
                    {
                        Debug.LogError("Effect not a holder");
                    }
                }
                BuildEffectHolder();

                break;
            case EffectType.DoneDamage:
                DoneDamageEffects.AddRange(effects);
                break;
            default:
                break;
        }

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
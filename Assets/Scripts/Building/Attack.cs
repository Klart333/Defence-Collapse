using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using Effects;
using System;

[System.Serializable]
public class Attack
{
    [OdinSerialize, NonSerialized]
    public List<IEffect> Effects;

    [OdinSerialize, NonSerialized]
    public List<IEffect> DoneDamageEffects = new List<IEffect>();

    private List<IEffectHolder> effectHolders;

    [ReadOnly, OdinSerialize]
    public List<IEffectHolder> EffectHolders => effectHolders ??= new List<IEffectHolder>();

    public Attack()
    {
        Effects = new List<IEffect>();
        DoneDamageEffects = new List<IEffect>();
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

        EffectHolders[^1].Perform(attacker);
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
        }
    }

    public void RemoveEffect(List<IEffect> effects, EffectType effectType)
    {
        switch (effectType)
        {
            case EffectType.Effect:
                foreach (IEffect effect in effects)
                {
                    Effects.Remove(effect);
                }
                break;
            case EffectType.Holder:
                for (int i = 0; i < effects.Count; i++)
                {
                    if (effects[i] is IEffectHolder holder)
                    {
                        effectHolders.Remove(holder);
                    }
                    else
                    {
                        Debug.LogError("Effect not a holder");
                    }
                }
                BuildEffectHolder();

                break;
            case EffectType.DoneDamage:
                foreach (IEffect effect in effects)
                {
                    DoneDamageEffects.Remove(effect);
                }
                break;
            default:
                break;
        }
    }

    private void BuildEffectHolder()
    {
        for (int i = 0; i < EffectHolders.Count; i++)
        {
            EffectHolders[i].Effects = i == 0 
                ? Effects 
                : new List<IEffect> { EffectHolders[i - 1] };
        }
    }
}
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Effects;
using System;

namespace Loot
{
    [Serializable]
    public class EffectModifier
    {
        [Title("Info")]
        [PreviewField]
        public Sprite Icon;

        public string Description;
        public string Title;

        public EffectType EffectType;

        [Title("Effects")]
        public List<IEffect> Effects;

        public EffectModifier()
        {
            Effects = new List<IEffect>();
        }
        
        public EffectModifier(Sprite icon, string description, string title, EffectType effectType, List<IEffect> effects)
        {
            Icon = icon;
            Description = description;
            Title = title;
            EffectType = effectType;
            Effects = effects;
        }

        public EffectModifier(EffectModifier copy)
        {
            Icon = copy.Icon;
            Description = copy.Description;
            Title = copy.Title;
            EffectType = copy.EffectType;
            Effects = new List<IEffect>();

            foreach (IEffect effect in copy.Effects)
            {
                if (effect is IEffectHolder holder)
                {
                    Effects.Add(holder.Clone() as IEffect);
                }
                else
                {
                    Effects.Add(effect);
                }
            }
        }
    }
}
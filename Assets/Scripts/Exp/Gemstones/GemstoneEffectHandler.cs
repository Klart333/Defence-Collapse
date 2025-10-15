using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Gameplay.Upgrades;
using UnityEngine;
using Effects;
using System;

namespace Exp.Gemstones
{
    public class GemstoneEffectHandler : MonoBehaviour
    {
        [SerializeField]
        private AppliedEffectsHandler appliedEffectsHandler;
    
        private ExpManager expManager;

        private void OnEnable()
        {
            GetExpManager().Forget();
        }

        private async UniTaskVoid GetExpManager()
        {
            expManager = await ExpManager.Get();

            ActivateGemstones(expManager.ActiveGemstones);
        }

        private void ActivateGemstones(List<Gemstone> gemstones)
        {
            Dictionary<Type, float> stats = new Dictionary<Type, float>();
            Dictionary<Type, StatIncreaseEffect> statIncreaseEffects = new Dictionary<Type, StatIncreaseEffect>();
            
            Dictionary<Type, IGemstoneEffect> uniqueGemEffects = new Dictionary<Type, IGemstoneEffect>();

            foreach (Gemstone activeGemstone in gemstones)
            {
                ActivateGemstoneEffect(activeGemstone);
            }
            
            foreach (StatIncreaseEffect effectCopy in statIncreaseEffects.Values)
            {
                if (effectCopy.IsAdditivePercent)
                {
                    effectCopy.ModifierValue += 1;
                }
                effectCopy.PerformEffect();
            }

            foreach (IGemstoneEffect effectCopy in uniqueGemEffects.Values)
            {
                if (effectCopy.IsAdditivePercent)
                {
                    effectCopy.ModifierValue += 1;
                }
                effectCopy.PerformEffect();
            }
            
            void ActivateGemstoneEffect(Gemstone gem)
            {
                foreach (IGemstoneEffect effect in gem.Effects)
                {
                    switch (effect)
                    {
                        case StatIncreaseEffect { Effect: IncreaseStatEffect stat } statEffect:
                        {
                            if (stats.TryGetValue(stat.statType.Type, out float value))
                            {
                                stats[stat.statType.Type] = effect.CumulativeType switch
                                {
                                    Modifier.ModifierType.Additive => value + effect.ModifierValue,
                                    Modifier.ModifierType.Multiplicative => value * effect.ModifierValue,
                                    _ => throw new ArgumentOutOfRangeException()
                                };
                                
                                statIncreaseEffects[stat.statType.Type].ModifierValue = stats[stat.statType.Type];
                            }
                            else
                            {
                                stats.Add(stat.statType.Type, statEffect.ModifierValue);
                                statIncreaseEffects.Add(stat.statType.Type, statEffect.Copy() as StatIncreaseEffect);
                            }
                        
                            continue;
                        }
                        default:
                            if (uniqueGemEffects.TryGetValue(effect.GetType(), out IGemstoneEffect gemEffect))
                            {
                                gemEffect.ModifierValue = effect.CumulativeType switch
                                {
                                    Modifier.ModifierType.Additive => effect.ModifierValue + gemEffect.ModifierValue,
                                    Modifier.ModifierType.Multiplicative => effect.ModifierValue * gemEffect.ModifierValue,
                                    _ => throw new ArgumentOutOfRangeException()
                                };
                            }
                            else
                            {
                                uniqueGemEffects.Add(effect.GetType(), effect.Copy());
                            }
                            continue;
                    }
                }
            }
        }
    }
}
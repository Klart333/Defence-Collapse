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
            Dictionary<StatType, float> stats = new Dictionary<StatType, float>();
            Dictionary<StatType, StatIncreaseEffect> statIncreaseEffects = new Dictionary<StatType, StatIncreaseEffect>();
            
            Dictionary<Type, IGemstoneEffect> uniqueGemEffects = new Dictionary<Type, IGemstoneEffect>();

            foreach (Gemstone activeGemstone in gemstones)
            {
                ActivateGemstoneEffect(activeGemstone);
            }
            
            foreach (StatIncreaseEffect effectCopy in statIncreaseEffects.Values)
            {
                effectCopy.Value += 1;
                effectCopy.PerformEffect();
            }

            foreach (IGemstoneEffect effectCopy in uniqueGemEffects.Values)
            {
                effectCopy.Value += 1;
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
                            if (!stats.ContainsKey(stat.StatType))
                            {
                                stats.Add(stat.StatType, statEffect.Value);
                                statIncreaseEffects.Add(stat.StatType, statEffect.Copy() as StatIncreaseEffect);
                            }
                            else
                            {
                                stats[stat.StatType] += statEffect.Value;
                                statIncreaseEffects[stat.StatType].Value = stats[stat.StatType];
                            }
                        
                            continue;
                        }
                        default:
                            if (uniqueGemEffects.TryGetValue(effect.GetType(), out IGemstoneEffect gemEffect))
                            {
                                gemEffect.Value += effect.Value;
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
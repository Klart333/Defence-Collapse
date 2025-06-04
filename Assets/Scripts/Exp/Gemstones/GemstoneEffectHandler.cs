using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Effects;
using Gameplay.Upgrades;
using UnityEngine;

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
            
            Dictionary<StatType, float> stats = new Dictionary<StatType, float>();
            Dictionary<StatType, StatIncreaseEffect> statIncreaseEffects = new Dictionary<StatType, StatIncreaseEffect>();

            foreach (Gemstone activeGemstone in expManager.ActiveGemstones)
            {
                ActivateGemstoneEffect(activeGemstone);
            }
            
            foreach (StatIncreaseEffect effectCopy in statIncreaseEffects.Values)
            {
                effectCopy.Value += 1;
                effectCopy.PerformEffect();
            }
            
            void ActivateGemstoneEffect(Gemstone gem)
            {
                foreach (IGemstoneEffect effect in gem.Effects)   
                {
                    if (effect is IGemstoneAppliedEffect appliedEffect)
                    {
                        if (appliedEffect is StatIncreaseEffect { Effect: IncreaseStatEffect stat } statEffect)
                        {
                            if (!stats.ContainsKey(stat.StatType))
                            {
                                stats.Add(stat.StatType, statEffect.Value);
                                StatIncreaseEffect effectCopy = new StatIncreaseEffect(statEffect); 
                                statIncreaseEffects.Add(stat.StatType, effectCopy);
                            }
                            else
                            {
                                stats[stat.StatType] += statEffect.Value;
                                statIncreaseEffects[stat.StatType].Value = stats[stat.StatType];
                            }
                        
                            continue;
                        }
                    
                        appliedEffect.PerformEffect(appliedEffectsHandler);
                        continue;
                    }
                
                    effect.PerformEffect();
                }
            }
        }
    }
}
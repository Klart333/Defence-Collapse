using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Text;
using UnityEngine;
using Effects;
using System;
using TMPro;

namespace Exp.Gemstones
{
    public class UIActiveGemstoneEffectDisplay : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI bonusText;

        [SerializeField]
        private StatNameUtility statNameUtility;
        
        [SerializeField]
        private GemstoneEffectDescriptions gemstoneEffectDescriptions;
        
        private ExpManager expManager;
        
        private void OnEnable()
        {
            GetExpManager().Forget();
        }

        private void OnDisable()
        {
            expManager.OnActiveGemstonesChanged -= UpdateText;
        }

        private async UniTaskVoid GetExpManager()
        {
            expManager = await ExpManager.Get();
            
            expManager.OnActiveGemstonesChanged += UpdateText;
            UpdateText();
        }

        private void UpdateText()
        {
            StringBuilder stringBuilder = new StringBuilder();
            Dictionary<StatType, float> stats = new Dictionary<StatType, float>();
            Dictionary<Type, float> uniqueEffects = new Dictionary<Type, float>();
            foreach (Gemstone gemstone in expManager.ActiveGemstones)
            {
                foreach (IGemstoneEffect effect in gemstone.Effects)
                {
                    switch (effect)
                    {
                        case StatIncreaseEffect { Effect: IncreaseStatEffect stat }:
                        {
                            if (stats.TryGetValue(stat.StatType, out float value))
                            {
                                stats[stat.StatType] = effect.CumulativeType switch
                                {
                                    Modifier.ModifierType.Additive => value + effect.ModifierValue,
                                    Modifier.ModifierType.Multiplicative => value * effect.ModifierValue,
                                    _ => throw new ArgumentOutOfRangeException()
                                };
                            }
                            else
                            {
                                stats.Add(stat.StatType, effect.ModifierValue);
                            }
                        
                            continue;
                        }
                        default:
                            if (uniqueEffects.TryGetValue(effect.GetType(), out float uniqueValue))
                            {
                                uniqueEffects[effect.GetType()] = effect.CumulativeType switch
                                {
                                   Modifier.ModifierType.Additive => uniqueValue + effect.ModifierValue,
                                   Modifier.ModifierType.Multiplicative => uniqueValue * effect.ModifierValue,
                                   _ => throw new ArgumentOutOfRangeException()
                                };
                            }
                            else
                            {
                                uniqueEffects.Add(effect.GetType(), effect.ModifierValue);
                            }
                            continue;
                    }
                }
                
            }

            foreach (KeyValuePair<StatType, float> stat in stats)
            {
                stringBuilder.AppendLine(statNameUtility.GetDescription(stat.Key, stat.Value));
            }

            foreach (KeyValuePair<Type, float> uniqueEffect in uniqueEffects)
            {
                stringBuilder.AppendLine(gemstoneEffectDescriptions.GetDescription(uniqueEffect.Key, uniqueEffect.Value));
            }
            
            bonusText.text = stringBuilder.ToString();
        }
    }
}
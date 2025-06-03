using Sirenix.Serialization;
using Sirenix.OdinInspector;
using Gameplay.Upgrades;
using UnityEngine;
using Effects;

namespace Exp.Gemstones
{
    public interface IGemstoneBakingEffect
    {
        public IGemstoneEffect GetEffect(int level);
    }
    
    [System.Serializable]
    public class StatIncreaseBakeEffect : IGemstoneBakingEffect
    {
        [Title("Curve")]
        [SerializeField]
        private AnimationCurve levelToGainCurve;

        [SerializeField]
        private float randomness = 0.05f;
        
        [Title("Description")]
        [SerializeField]
        private string description = "• {0}x Attack Speed";
        
        [Title("Effect")]
        [SerializeField]
        private CategoryType appliedCategory;
        
        [OdinSerialize]
        private IEffect effect;

        public float GetEffectValue(int level)
        {
            return 1.0f + levelToGainCurve.Evaluate(level) * UnityEngine.Random.Range(1.0f - randomness, 1.0f + randomness);
        }

        public IGemstoneEffect GetEffect(int level)
        {
            if (effect is not IncreaseStatEffect increaseStatEffect)
            {
                Debug.LogError("Requires an IncreaseStatEffect");
                return null;
            }

            float value = GetEffectValue(level);
            return new StatIncreaseEffect
            {
                AppliedCategory = appliedCategory,
                Value = value,
                EffectDescription = string.Format(description, value.ToString("N")),
                Effect = new IncreaseStatEffect
                {
                    ModifierType = increaseStatEffect.ModifierType,
                    StatType = increaseStatEffect.StatType,
                    ModifierValue = value,
                }
            };
        }
    }

}
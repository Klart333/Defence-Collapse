using System;
using Random = System.Random;
using Sirenix.Serialization;
using Sirenix.OdinInspector;
using Gameplay.Upgrades;
using UnityEngine;
using Effects;

namespace Exp.Gemstones
{
    public interface IGemstoneBakingEffect
    {
        public IGemstoneEffect GetEffect(int level, Random random);
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
        private StatNameUtility statNameUtility;
        
        [Title("Effect")]
        [SerializeField]
        private CategoryType appliedCategory;
        
        [OdinSerialize]
        private IEffect effect;

        public float GetEffectValue(int level, Random random)
        {
            float value = levelToGainCurve.Evaluate(level);
            return value + value * (float)(random.NextDouble() * 2.0f - 1.0f) * randomness;
        }

        public IGemstoneEffect GetEffect(int level, Random random)
        {
            if (effect is not IncreaseStatEffect increaseStatEffect)
            {
                Debug.LogError("Requires an IncreaseStatEffect");
                return null;
            }

            float value = GetEffectValue(level, random);
            return new StatIncreaseEffect
            {
                AppliedCategory = appliedCategory,
                Value = value,
                EffectDescription = statNameUtility.GetDescription(increaseStatEffect.StatType, value),
                Effect = new IncreaseStatEffect
                {
                    ModifierType = increaseStatEffect.ModifierType,
                    StatType = increaseStatEffect.StatType,
                    ModifierValue = value,
                }
            };
        }
    }

    [System.Serializable]
    public class GameSpeedIncreaseBakeEffect : IGemstoneBakingEffect
    {
        [Title("Curve")]
        [SerializeField]
        private AnimationCurve levelToGainCurve;

        [SerializeField]
        private float randomness = 0.05f;
        
        [Title("Description")]
        [SerializeField]
        private string description = "+{0} Game Speed";
        
        public float GetEffectValue(int level, Random random)
        {
            float value = levelToGainCurve.Evaluate(level);
            return value + value * (float)(random.NextDouble() * 2.0f - 1.0f) * randomness;        
        }

        public IGemstoneEffect GetEffect(int level, Random random)
        {
            float value = GetEffectValue(level, random);
            return new IncreaseGameSpeedEffect
            {
                Value = value,
                EffectDescription = string.Format(description, value.ToString("P")),
            };
        }
    }
}
using Random = System.Random;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Gameplay.Upgrades;
using UnityEngine;
using Effects;
using System;

namespace Exp.Gemstones
{
    public interface IGemstoneBakingEffect
    {
        public IGemstoneEffect GetEffect(int level, Random random);
    }
    
    [Serializable]
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
        private bool isAdditivePercent = true;
        
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
                IsAdditivePercent = isAdditivePercent,
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

    [Serializable]
    public class GameSpeedIncreaseBakeEffect : IGemstoneBakingEffect
    {
        [Title("Curve")]
        [SerializeField]
        private AnimationCurve levelToGainCurve;

        [SerializeField]
        private float randomness = 0.05f;
        
        [Title("Description")]
        [SerializeField]
        private GemstoneEffectDescriptions effectDescriptions;
        
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
                EffectDescription = effectDescriptions.GetDescription(typeof(IncreaseGameSpeedEffect), value),
            };
        }
    }
    
    [Serializable]
    public class MoneyIncreaseBakeEffect : IGemstoneBakingEffect
    {
        [Title("Curve")]
        [SerializeField]
        private AnimationCurve levelToGainCurve;

        [SerializeField]
        private float randomness = 0.05f;
        
        [Title("Description")]
        [SerializeField]
        private GemstoneEffectDescriptions effectDescriptions;
        
        public float GetEffectValue(int level, Random random)
        {
            float value = levelToGainCurve.Evaluate(level);
            return value + value * (float)(random.NextDouble() * 2.0f - 1.0f) * randomness;        
        }

        public IGemstoneEffect GetEffect(int level, Random random)
        {
            float value = GetEffectValue(level, random);
            return new IncreaseMoneyEffect
            {
                Value = value,
                EffectDescription = effectDescriptions.GetDescription(typeof(IncreaseMoneyEffect), value),
            };
        }
    }
    
    [Serializable]
    public class ExpIncreaseBakeEffect : IGemstoneBakingEffect
    {
        [Title("Curve")]
        [SerializeField]
        private AnimationCurve levelToGainCurve;

        [SerializeField]
        private float randomness = 0.05f;
        
        [Title("Description")]
        [SerializeField]
        private GemstoneEffectDescriptions effectDescriptions;
        
        public float GetEffectValue(int level, Random random)
        {
            float value = levelToGainCurve.Evaluate(level);
            return value + value * (float)(random.NextDouble() * 2.0f - 1.0f) * randomness;        
        }

        public IGemstoneEffect GetEffect(int level, Random random)
        {
            float value = GetEffectValue(level, random);
            return new IncreaseExpEffect
            {
                Value = value,
                EffectDescription = effectDescriptions.GetDescription(typeof(IncreaseExpEffect), value),
            };
        }
    }
    
    [Serializable]
    public class CategoryDamageIncreaseBakeEffect : IGemstoneBakingEffect
    {
        [Title("Curve")]
        [SerializeField]
        private AnimationCurve levelToGainCurve;

        [SerializeField]
        private float randomness = 0.05f;
        
        [Title("Description")]
        [SerializeField]
        private GemstoneEffectDescriptions effectDescriptions;
        
        [Title("Category")]
        [SerializeField]
        private CategoryType appliedCategory;
        
        public float GetEffectValue(int level, Random random)
        {
            float value = levelToGainCurve.Evaluate(level);
            return value + value * (float)(random.NextDouble() * 2.0f - 1.0f) * randomness;        
        }

        public IGemstoneEffect GetEffect(int level, Random random)
        {
            float value = GetEffectValue(level, random);

            return appliedCategory switch
            {
                CategoryType.Projectile => new IncreaseProjectileDamageEffect
                {
                    Value = value,
                    EffectDescription = effectDescriptions.GetDescription(typeof(IncreaseProjectileDamageEffect), value),
                },
                CategoryType.AoE => new IncreaseSplashDamageEffect
                {
                    Value = value,
                    EffectDescription = effectDescriptions.GetDescription(typeof(IncreaseSplashDamageEffect), value),
                },
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
    
    [Serializable]
    public class WallStatIncreaseBakeEffect : IGemstoneBakingEffect
    {
        [Title("Curve")]
        [SerializeField]
        private AnimationCurve levelToGainCurve;

        [SerializeField]
        private float randomness = 0.05f;
        
        [Title("Description")]
        [SerializeField]
        private GemstoneEffectDescriptions effectDescriptions;
        
        [Title("Category")]
        [SerializeField]
        private StatType statType;
        
        public float GetEffectValue(int level, Random random)
        {
            float value = levelToGainCurve.Evaluate(level);
            return value + value * (float)(random.NextDouble() * 2.0f - 1.0f) * randomness;        
        }

        public IGemstoneEffect GetEffect(int level, Random random)
        {
            float value = GetEffectValue(level, random);

            return statType switch
            {
                StatType.MaxHealth => new IncreaseWallHealthEffect()
                {
                    Value = value,
                    EffectDescription = effectDescriptions.GetDescription(typeof(IncreaseWallHealthEffect), value),
                },
                StatType.Healing => new IncreaseWallHealingEffect
                {
                    Value = value,
                    EffectDescription = effectDescriptions.GetDescription(typeof(IncreaseWallHealingEffect), value),
                },
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
    
    [Serializable]
    public class BarricadeStatIncreaseBakeEffect : IGemstoneBakingEffect
    {
        [Title("Curve")]
        [SerializeField]
        private AnimationCurve levelToGainCurve;

        [SerializeField]
        private float randomness = 0.05f;
        
        [Title("Description")]
        [SerializeField]
        private GemstoneEffectDescriptions effectDescriptions;
        
        [Title("Category")]
        [SerializeField]
        private StatType statType;
        
        public float GetEffectValue(int level, Random random)
        {
            float value = levelToGainCurve.Evaluate(level);
            return value + value * (float)(random.NextDouble() * 2.0f - 1.0f) * randomness;        
        }

        public IGemstoneEffect GetEffect(int level, Random random)
        {
            float value = GetEffectValue(level, random);

            return statType switch
            {
                StatType.MaxHealth => new IncreaseBarricadeHealthEffect()
                {
                    Value = value,
                    EffectDescription = effectDescriptions.GetDescription(typeof(IncreaseBarricadeHealthEffect), value),
                },
                StatType.Healing => new IncreaseBarricadeHealingEffect
                {
                    Value = value,
                    EffectDescription = effectDescriptions.GetDescription(typeof(IncreaseBarricadeHealingEffect), value),
                },
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
    
    [Serializable]
    public class EnemySpeedModifierBakeEffect : IGemstoneBakingEffect
    {
        [Title("Curve")]
        [SerializeField]
        private AnimationCurve levelToGainCurve;

        [SerializeField]
        private float randomness = 0.05f;
        
        [Title("Description")]
        [SerializeField]
        private GemstoneEffectDescriptions effectDescriptions;
        
        public float GetEffectValue(int level, Random random)
        {
            float value = levelToGainCurve.Evaluate(level);
            return value + value * (float)(random.NextDouble() * 2.0f - 1.0f) * randomness;        
        }

        public IGemstoneEffect GetEffect(int level, Random random)
        {
            float value = GetEffectValue(level, random);
            return new EnemySpeedModifierEffect()
            {
                Value = value,
                EffectDescription = effectDescriptions.GetDescription(typeof(EnemySpeedModifierEffect), value),
            };
        }
    }
    
    [Serializable]
    public class EnemyDamageModifierBakeEffect : IGemstoneBakingEffect
    {
        [Title("Curve")]
        [SerializeField]
        private AnimationCurve levelToGainCurve;

        [SerializeField]
        private float randomness = 0.05f;
        
        [Title("Description")]
        [SerializeField]
        private GemstoneEffectDescriptions effectDescriptions;
        
        public float GetEffectValue(int level, Random random)
        {
            float value = levelToGainCurve.Evaluate(level);
            return value + value * (float)(random.NextDouble() * 2.0f - 1.0f) * randomness;        
        }

        public IGemstoneEffect GetEffect(int level, Random random)
        {
            float value = GetEffectValue(level, random);
            return new EnemyDamageModifierEffect()
            {
                Value = value,
                EffectDescription = effectDescriptions.GetDescription(typeof(EnemyDamageModifierEffect), value),
            };
        }
    }
}
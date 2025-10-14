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
                ModifierValue = value,
                EffectDescription = statNameUtility.GetDescription(increaseStatEffect.StatTypeType.StatType, value),
                Effect = new IncreaseStatEffect
                {
                    ModifierType = increaseStatEffect.ModifierType,
                    StatTypeType = increaseStatEffect.StatTypeType,
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
                ModifierValue = value,
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
                ModifierValue = value,
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
                ModifierValue = value,
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
                    ModifierValue = value,
                    EffectDescription = effectDescriptions.GetDescription(typeof(IncreaseProjectileDamageEffect), value),
                },
                CategoryType.AoE => new IncreaseSplashDamageEffect
                {
                    ModifierValue = value,
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
        private StatTypeType statTypeType;
        
        public float GetEffectValue(int level, Random random)
        {
            float value = levelToGainCurve.Evaluate(level);
            return value + value * (float)(random.NextDouble() * 2.0f - 1.0f) * randomness;        
        }

        public IGemstoneEffect GetEffect(int level, Random random)
        {
            float value = GetEffectValue(level, random);

            if (statTypeType.StatType.IsEquivalentTo(typeof(MaxHealthStat)))
            {
                return new IncreaseWallHealthEffect()
                {
                    ModifierValue = value,
                    EffectDescription = effectDescriptions.GetDescription(typeof(IncreaseBarricadeHealthEffect), value),
                };
            }

            if (statTypeType.StatType.IsEquivalentTo(typeof(HealingStat)))
            {
                return new IncreaseWallHealingEffect
                {
                    ModifierValue = value,
                    EffectDescription = effectDescriptions.GetDescription(typeof(IncreaseBarricadeHealingEffect), value),
                };
            }

            throw new ArgumentOutOfRangeException();
        }
    }
    
    [Serializable, Obsolete]
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
        private StatTypeType statTypeType;
        
        public float GetEffectValue(int level, Random random)
        {
            float value = levelToGainCurve.Evaluate(level);
            return value + value * (float)(random.NextDouble() * 2.0f - 1.0f) * randomness;        
        }

        public IGemstoneEffect GetEffect(int level, Random random)
        {
            float value = GetEffectValue(level, random);

            if (statTypeType.StatType.IsEquivalentTo(typeof(MaxHealthStat)))
            {
                return new IncreaseBarricadeHealthEffect()
                {
                    ModifierValue = value,
                    EffectDescription = effectDescriptions.GetDescription(typeof(IncreaseBarricadeHealthEffect), value),
                };
            }

            if (statTypeType.StatType.IsEquivalentTo(typeof(HealingStat)))
            {
                return new IncreaseBarricadeHealingEffect
                {
                    ModifierValue = value,
                    EffectDescription = effectDescriptions.GetDescription(typeof(IncreaseBarricadeHealingEffect), value),
                };
            }

            throw new ArgumentOutOfRangeException();
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
                ModifierValue = value,
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
                ModifierValue = value,
                EffectDescription = effectDescriptions.GetDescription(typeof(EnemyDamageModifierEffect), value),
            };
        }
    }
}
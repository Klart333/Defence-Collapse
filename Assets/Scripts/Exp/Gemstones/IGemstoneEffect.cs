using Gameplay.Upgrades;
using UnityEngine;
using Gameplay;
using Effects;
using Gameplay.Money;
using Gameplay.Upgrades.ECS;
using Health;

namespace Exp.Gemstones
{
    public interface IGemstoneEffect
    {
        public void PerformEffect();
        public string GetDescription();
        
        public float Value { get; set; }
        public IGemstoneEffect Copy();
    }
    
    public class StatIncreaseEffect : IGemstoneEffect
    {
        public CategoryType AppliedCategory;
        public IEffect Effect;
        
        public string EffectDescription;
        public float Value { get; set; }

        public StatIncreaseEffect() { }
        
        public StatIncreaseEffect(StatIncreaseEffect statEffect)
        {
            EffectDescription = statEffect.EffectDescription;
            AppliedCategory = statEffect.AppliedCategory;
            Effect = statEffect.Effect;
            Value = statEffect.Value;
        }

        public void PerformEffect() => PerformEffect(Object.FindFirstObjectByType<AppliedEffectsHandler>());
        
        public void PerformEffect(AppliedEffectsHandler handler)
        {
            Effect.ModifierValue = Value;
            handler.AddUpgradeEffect(AppliedCategory, Effect);
        }
        
        public string GetDescription()
        {
            return EffectDescription;
        }

        public IGemstoneEffect Copy() => new StatIncreaseEffect(this);
    }
    
    public class IncreaseGameSpeedEffect : IGemstoneEffect
    {
        public string EffectDescription;
        public float Value { get; set; }

        public IncreaseGameSpeedEffect() { }
        public IncreaseGameSpeedEffect(IncreaseGameSpeedEffect copy)
        {
            EffectDescription = copy.EffectDescription;
            Value = copy.Value;
        }

        public void PerformEffect()
        {
            GameSpeedManager.Instance.AddModifier(new Modifier
            {
                Value = Value,
                Type = Modifier.ModifierType.Multiplicative
            });
        }

        public string GetDescription() => EffectDescription;
        public IGemstoneEffect Copy() => new IncreaseGameSpeedEffect(this);
    }
    
    public class IncreaseMoneyEffect : IGemstoneEffect
    {
        public string EffectDescription;
        public float Value { get; set; }

        public IncreaseMoneyEffect() { }
        public IncreaseMoneyEffect(IncreaseMoneyEffect copy)
        {
            EffectDescription = copy.EffectDescription;
            Value = copy.Value;
        }

        public void PerformEffect()
        {
            MoneyManager.Instance.MoneyMultiplier.AddModifier(new Modifier
            {
                Value = Value,
                Type = Modifier.ModifierType.Multiplicative
            });
        }

        public string GetDescription() => EffectDescription;
        public IGemstoneEffect Copy() => new IncreaseMoneyEffect(this);
    }
    
    public class IncreaseExpEffect : IGemstoneEffect
    {
        public string EffectDescription;
        public float Value { get; set; }

        public IncreaseExpEffect() { }
        public IncreaseExpEffect(IncreaseExpEffect copy)
        {
            EffectDescription = copy.EffectDescription;
            Value = copy.Value;
        }

        public void PerformEffect()
        {
            ExpManager.Instance.ExpMultiplier.AddModifier(new Modifier
            {
                Value = Value,
                Type = Modifier.ModifierType.Multiplicative
            });
        }

        public string GetDescription() => EffectDescription;
        public IGemstoneEffect Copy() => new IncreaseExpEffect(this);
    }
    
    public class IncreaseProjectileDamageEffect : IGemstoneEffect
    {
        public string EffectDescription;
        public float Value { get; set; }

        public IncreaseProjectileDamageEffect() { }
        public IncreaseProjectileDamageEffect(IncreaseProjectileDamageEffect copy)
        {
            EffectDescription = copy.EffectDescription;
            Value = copy.Value;
        }

        public void PerformEffect()
        {
            GameDataManager.Instance.IncreaseGameData(new MultiplyDamageComponent
            {
                AppliedCategory = CategoryType.Projectile,
                AppliedHealthType = HealthType.Health | HealthType.Armor | HealthType.Shield,
                DamageMultiplier = Value,
            });
        }

        public string GetDescription() => EffectDescription;
        public IGemstoneEffect Copy() => new IncreaseProjectileDamageEffect(this);
    }
    
    public class IncreaseSplashDamageEffect : IGemstoneEffect
    {
        public string EffectDescription;
        public float Value { get; set; }

        public IncreaseSplashDamageEffect() { }
        public IncreaseSplashDamageEffect(IncreaseSplashDamageEffect copy)
        {
            EffectDescription = copy.EffectDescription;
            Value = copy.Value;
        }

        public void PerformEffect()
        {
            GameDataManager.Instance.IncreaseGameData(new MultiplyDamageComponent
            {
                AppliedCategory = CategoryType.AoE,
                AppliedHealthType = HealthType.Health | HealthType.Armor | HealthType.Shield,
                DamageMultiplier = Value,
            });
        }

        public string GetDescription() => EffectDescription;
        public IGemstoneEffect Copy() => new IncreaseSplashDamageEffect(this);
    }
}
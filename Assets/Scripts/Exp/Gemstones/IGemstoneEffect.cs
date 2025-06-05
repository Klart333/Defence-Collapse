using Gameplay.Upgrades.ECS;
using Gameplay.Upgrades;
using Gameplay.Money;
using UnityEngine;
using Enemy.ECS;
using Gameplay;
using Effects;
using Health;

namespace Exp.Gemstones
{
    public interface IGemstoneEffect
    {
        public float Value { get; set; }
        public Modifier.ModifierType CumulativeType { get; }
        public bool IsAdditivePercent { get; }
        
        public void PerformEffect();
        public string GetDescription();
        public IGemstoneEffect Copy();
    }
    
    public class StatIncreaseEffect : IGemstoneEffect
    {
        public CategoryType AppliedCategory;
        public IEffect Effect;
        
        public string EffectDescription;
        public float Value { get; set; }
        public Modifier.ModifierType CumulativeType => Modifier.ModifierType.Additive;
        public bool IsAdditivePercent { get; set; }

        public StatIncreaseEffect() { }
        
        public StatIncreaseEffect(StatIncreaseEffect statEffect)
        {
            IsAdditivePercent = statEffect.IsAdditivePercent;
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
        public Modifier.ModifierType CumulativeType => Modifier.ModifierType.Additive;
        public bool IsAdditivePercent => true;

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
        public Modifier.ModifierType CumulativeType => Modifier.ModifierType.Additive;
        public bool IsAdditivePercent => true;

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
        public Modifier.ModifierType CumulativeType => Modifier.ModifierType.Additive;
        public bool IsAdditivePercent => true;

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
        public Modifier.ModifierType CumulativeType => Modifier.ModifierType.Additive;
        public bool IsAdditivePercent => true;

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
        public Modifier.ModifierType CumulativeType => Modifier.ModifierType.Additive;
        public bool IsAdditivePercent => true;

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

    #region GameData

    public class IncreaseWallHealthEffect : IGemstoneEffect
    {
        public string EffectDescription;
        public float Value { get; set; }
        public Modifier.ModifierType CumulativeType => Modifier.ModifierType.Additive;
        public bool IsAdditivePercent => true;

        public IncreaseWallHealthEffect() { }
        public IncreaseWallHealthEffect(IncreaseWallHealthEffect copy)
        {
            EffectDescription = copy.EffectDescription;
            Value = copy.Value;
        }

        public void PerformEffect()
        {
            GameData.WallHealthMultiplier.AddModifier(new Modifier
            {
                Value = Value,
                Type = Modifier.ModifierType.Multiplicative
            });
        }

        public string GetDescription() => EffectDescription;
        public IGemstoneEffect Copy() => new IncreaseWallHealthEffect(this);
    }

    
    public class IncreaseWallHealingEffect : IGemstoneEffect
    {
        public string EffectDescription;
        public float Value { get; set; }
        public Modifier.ModifierType CumulativeType => Modifier.ModifierType.Additive;
        public bool IsAdditivePercent => false;

        public IncreaseWallHealingEffect() { }
        public IncreaseWallHealingEffect(IncreaseWallHealingEffect copy)
        {
            EffectDescription = copy.EffectDescription;
            Value = copy.Value;
        }

        public void PerformEffect()
        {
            GameData.WallHealing.AddModifier(new Modifier
            {
                Value = Value,
                Type = Modifier.ModifierType.Additive
            });
        }

        public string GetDescription() => EffectDescription;
        public IGemstoneEffect Copy() => new IncreaseWallHealingEffect(this);
    }
    
    public class IncreaseBarricadeHealthEffect : IGemstoneEffect
    {
        public string EffectDescription;
        public float Value { get; set; }
        public Modifier.ModifierType CumulativeType => Modifier.ModifierType.Additive;
        public bool IsAdditivePercent => true;

        public IncreaseBarricadeHealthEffect() { }
        public IncreaseBarricadeHealthEffect(IncreaseBarricadeHealthEffect copy)
        {
            EffectDescription = copy.EffectDescription;
            Value = copy.Value;
        }

        public void PerformEffect()
        {
            GameData.BarricadeHealthMultiplier.AddModifier(new Modifier
            {
                Value = Value,
                Type = Modifier.ModifierType.Multiplicative
            });
        }

        public string GetDescription() => EffectDescription;
        public IGemstoneEffect Copy() => new IncreaseBarricadeHealthEffect(this);
    }

    
    public class IncreaseBarricadeHealingEffect : IGemstoneEffect
    {
        public string EffectDescription;
        public float Value { get; set; }
        public Modifier.ModifierType CumulativeType => Modifier.ModifierType.Additive;
        public bool IsAdditivePercent => false;

        public IncreaseBarricadeHealingEffect() { }
        public IncreaseBarricadeHealingEffect(IncreaseBarricadeHealingEffect copy)
        {
            EffectDescription = copy.EffectDescription;
            Value = copy.Value;
        }

        public void PerformEffect()
        {
            GameData.BarricadeHealing.AddModifier(new Modifier
            {
                Value = Value,
                Type = Modifier.ModifierType.Additive
            });
        }

        public string GetDescription() => EffectDescription;
        public IGemstoneEffect Copy() => new IncreaseBarricadeHealingEffect(this);
    }
    
    #endregion
    
    public class EnemySpeedModifierEffect : IGemstoneEffect
    {
        public string EffectDescription;
        public float Value { get; set; }
        public Modifier.ModifierType CumulativeType => Modifier.ModifierType.Multiplicative;
        public bool IsAdditivePercent => false;

        public EnemySpeedModifierEffect() { }
        public EnemySpeedModifierEffect(EnemySpeedModifierEffect copy)
        {
            EffectDescription = copy.EffectDescription;
            Value = copy.Value;
        }

        public void PerformEffect()
        {
            GameDataManager.Instance.IncreaseGameData(new EnemySpeedModifierComponent
            {
                SpeedMultiplier = Value
            });
        }

        public string GetDescription() => EffectDescription;
        public IGemstoneEffect Copy() => new EnemySpeedModifierEffect(this);
    }
    
    public class EnemyDamageModifierEffect : IGemstoneEffect
    {
        public string EffectDescription;
        public float Value { get; set; }
        public Modifier.ModifierType CumulativeType => Modifier.ModifierType.Multiplicative;
        public bool IsAdditivePercent => false;

        public EnemyDamageModifierEffect() { }
        public EnemyDamageModifierEffect(EnemyDamageModifierEffect copy)
        {
            EffectDescription = copy.EffectDescription;
            Value = copy.Value;
        }

        public void PerformEffect()
        {
            GameDataManager.Instance.IncreaseGameData(new EnemyDamageModifierComponent
            {
                DamageMultiplier = Value
            });
        }

        public string GetDescription() => EffectDescription;
        public IGemstoneEffect Copy() => new EnemyDamageModifierEffect(this);
    }
}
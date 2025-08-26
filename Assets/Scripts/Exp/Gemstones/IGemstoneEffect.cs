using Object = UnityEngine.Object;
using Gameplay.Upgrades.ECS;
using Gameplay.Upgrades;
using Gameplay.Money;
using Enemy.ECS;
using Gameplay;
using Effects;
using Health;
using System;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

namespace Exp.Gemstones
{
    public interface IGemstoneEffect
    {
        public Modifier.ModifierType CumulativeType { get; }
        public bool IsAdditivePercent { get; }
        public float ModifierValue { get; set; }
        
        public void PerformEffect();
        public string GetDescription();
        public IGemstoneEffect Copy();
    }
    
    [Serializable]
    public class StatIncreaseEffect : IEffect, IGemstoneEffect
    {
        [OdinSerialize, Title("Effect Modifier Value")]
        public float ModifierValue { get; set; }

        [Title("Effect")]
        public CategoryType AppliedCategory;
        public IEffect Effect;
        
        private AppliedEffectsHandler appliedEffectsHandler;
        
        public string EffectDescription { get; set; }
        public Modifier.ModifierType CumulativeType => Modifier.ModifierType.Additive;
        public bool IsAdditivePercent { get; set; }

        public StatIncreaseEffect() { }
        
        public StatIncreaseEffect(StatIncreaseEffect statEffect)
        {
            IsAdditivePercent = statEffect.IsAdditivePercent;
            EffectDescription = statEffect.EffectDescription;
            AppliedCategory = statEffect.AppliedCategory;
            Effect = statEffect.Effect;
            ModifierValue = statEffect.ModifierValue;
        }

        public void Perform(IAttacker attacker) => PerformEffect();
        
        public void PerformEffect()
        {
            appliedEffectsHandler ??= Object.FindFirstObjectByType<AppliedEffectsHandler>();
            PerformEffect(appliedEffectsHandler);
        }
        
        public void PerformEffect(AppliedEffectsHandler handler)
        {
            Effect.ModifierValue = ModifierValue;
            handler.AddUpgradeEffect(AppliedCategory, Effect);
        }
        
        public string GetDescription()
        {
            return EffectDescription;
        }

        public IGemstoneEffect Copy() => new StatIncreaseEffect(this);
    }
    
    [Serializable]
    public class IncreaseGameSpeedEffect : IEffect, IGemstoneEffect
    {
        [OdinSerialize, Title("Game Speed Multiplier")]
        public float ModifierValue { get; set; }
        public string EffectDescription { get; set; }
        public Modifier.ModifierType CumulativeType => Modifier.ModifierType.Additive;
        public bool IsAdditivePercent => true;

        public IncreaseGameSpeedEffect() { }
        public IncreaseGameSpeedEffect(IncreaseGameSpeedEffect copy)
        {
            EffectDescription = copy.EffectDescription;
            ModifierValue = copy.ModifierValue;
        }
        
        public void Perform(IAttacker attacker) => PerformEffect();

        public void PerformEffect()
        {
            GameSpeedManager.Instance.AddModifier(new Modifier
            {
                Value = ModifierValue,
                Type = Modifier.ModifierType.Multiplicative
            });
        }

        public string GetDescription() => EffectDescription;
        public IGemstoneEffect Copy() => new IncreaseGameSpeedEffect(this);
    }
    
    [Serializable]
    public class IncreaseMoneyEffect : IEffect, IGemstoneEffect
    {
        [OdinSerialize, Title("Money Multiplier")]
        public float ModifierValue { get; set; }
        public string EffectDescription { get; set; }
        public Modifier.ModifierType CumulativeType => Modifier.ModifierType.Additive;
        public bool IsAdditivePercent => true;

        public IncreaseMoneyEffect() { }
        public IncreaseMoneyEffect(IncreaseMoneyEffect copy)
        {
            EffectDescription = copy.EffectDescription;
            ModifierValue = copy.ModifierValue;
        }

        public void Perform(IAttacker attacker) => PerformEffect();

        public void PerformEffect()
        {
            MoneyManager.Instance.MoneyMultiplier.AddModifier(new Modifier
            {
                Value = ModifierValue,
                Type = Modifier.ModifierType.Multiplicative
            });
        }

        public string GetDescription() => EffectDescription;
        public IGemstoneEffect Copy() => new IncreaseMoneyEffect(this);
    }
    
    [Serializable]
    public class IncreaseExpEffect : IEffect, IGemstoneEffect
    {
        [OdinSerialize, Title("Exp Multiplier")]
        public float ModifierValue { get; set; }
        public string EffectDescription { get; set; }
        public Modifier.ModifierType CumulativeType => Modifier.ModifierType.Additive;
        public bool IsAdditivePercent => true;

        public IncreaseExpEffect() { }
        public IncreaseExpEffect(IncreaseExpEffect copy)
        {
            EffectDescription = copy.EffectDescription;
            ModifierValue = copy.ModifierValue;
        }
        
        public void Perform(IAttacker attacker) => PerformEffect();

        public void PerformEffect()
        {
            ExpManager.Instance.ExpMultiplier.AddModifier(new Modifier
            {
                Value = ModifierValue,
                Type = Modifier.ModifierType.Multiplicative
            });
        }

        public string GetDescription() => EffectDescription;
        public IGemstoneEffect Copy() => new IncreaseExpEffect(this);
    }
    
    [Serializable]
    public class IncreaseProjectileDamageEffect : IEffect, IGemstoneEffect
    {
        [OdinSerialize, Title("Damage Multiplier")]
        public float ModifierValue { get; set; }
        public string EffectDescription { get; set; }
        public Modifier.ModifierType CumulativeType => Modifier.ModifierType.Additive;
        public bool IsAdditivePercent => true;

        public IncreaseProjectileDamageEffect() { }
        public IncreaseProjectileDamageEffect(IncreaseProjectileDamageEffect copy)
        {
            EffectDescription = copy.EffectDescription;
            ModifierValue = copy.ModifierValue;
        }

        public void Perform(IAttacker attacker) => PerformEffect();

        public void PerformEffect()
        {
            GameDataManager.Instance.IncreaseGameData(new MultiplyDamageComponent
            {
                AppliedCategory = CategoryType.Projectile,
                AppliedHealthType = HealthType.Health | HealthType.Armor | HealthType.Shield,
                DamageMultiplier = ModifierValue,
            });
        }

        public string GetDescription() => EffectDescription;
        public IGemstoneEffect Copy() => new IncreaseProjectileDamageEffect(this);
    }
    
    [Serializable]
    public class IncreaseSplashDamageEffect : IEffect, IGemstoneEffect
    {
        [OdinSerialize, Title("Damage Multiplier")]
        public float ModifierValue { get; set; }
        public string EffectDescription { get; set; }
        public Modifier.ModifierType CumulativeType => Modifier.ModifierType.Additive;
        public bool IsAdditivePercent => true;

        public IncreaseSplashDamageEffect() { }
        public IncreaseSplashDamageEffect(IncreaseSplashDamageEffect copy)
        {
            EffectDescription = copy.EffectDescription;
            ModifierValue = copy.ModifierValue;
        }
        
        public void Perform(IAttacker attacker) => PerformEffect();

        public void PerformEffect()
        {
            GameDataManager.Instance.IncreaseGameData(new MultiplyDamageComponent
            {
                AppliedCategory = CategoryType.AoE,
                AppliedHealthType = HealthType.Health | HealthType.Armor | HealthType.Shield,
                DamageMultiplier = ModifierValue,
            });
        }

        public string GetDescription() => EffectDescription;
        public IGemstoneEffect Copy() => new IncreaseSplashDamageEffect(this);
    }

    #region GameData

    [Serializable]
    public class IncreaseWallHealthEffect : IEffect, IGemstoneEffect
    {
        [OdinSerialize, Title("Extra Wall Health")]
        public float ModifierValue { get; set; }
        public string EffectDescription { get; set; }
        public Modifier.ModifierType CumulativeType => Modifier.ModifierType.Additive;
        public bool IsAdditivePercent => true;

        public IncreaseWallHealthEffect() { }
        public IncreaseWallHealthEffect(IncreaseWallHealthEffect copy)
        {
            EffectDescription = copy.EffectDescription;
            ModifierValue = copy.ModifierValue;
        }
        
        public void Perform(IAttacker attacker) => PerformEffect();

        public void PerformEffect()
        {
            GameData.WallHealthMultiplier.AddModifier(new Modifier
            {
                Value = ModifierValue,
                Type = Modifier.ModifierType.Multiplicative
            });
        }

        public string GetDescription() => EffectDescription;
        public IGemstoneEffect Copy() => new IncreaseWallHealthEffect(this);
    }
    
    [Serializable]
    public class IncreaseWallHealingEffect : IEffect, IGemstoneEffect
    {
        [OdinSerialize, Title("Extra Wall Healing")]
        public float ModifierValue { get; set; }
        public string EffectDescription { get; set; }
        public Modifier.ModifierType CumulativeType => Modifier.ModifierType.Additive;
        public bool IsAdditivePercent => false;

        public IncreaseWallHealingEffect() { }
        public IncreaseWallHealingEffect(IncreaseWallHealingEffect copy)
        {
            EffectDescription = copy.EffectDescription;
            ModifierValue = copy.ModifierValue;
        }

        public void Perform(IAttacker attacker) => PerformEffect();

        public void PerformEffect()
        {
            GameData.WallHealing.AddModifier(new Modifier
            {
                Value = ModifierValue,
                Type = Modifier.ModifierType.Additive
            });
        }

        public string GetDescription() => EffectDescription;
        public IGemstoneEffect Copy() => new IncreaseWallHealingEffect(this);
    }
    
    [Serializable]
    public class IncreaseBarricadeHealthEffect : IEffect, IGemstoneEffect
    {
        [OdinSerialize, Title("Extra Barricade Health")]
        public float ModifierValue { get; set; }
        public string EffectDescription { get; set; }
        public Modifier.ModifierType CumulativeType => Modifier.ModifierType.Additive;
        public bool IsAdditivePercent => true;

        public IncreaseBarricadeHealthEffect() { }
        public IncreaseBarricadeHealthEffect(IncreaseBarricadeHealthEffect copy)
        {
            EffectDescription = copy.EffectDescription;
            ModifierValue = copy.ModifierValue;
        }

        public void Perform(IAttacker attacker) => PerformEffect();
        
        public void PerformEffect()
        {
            GameData.BarricadeHealthMultiplier.AddModifier(new Modifier
            {
                Value = ModifierValue,
                Type = Modifier.ModifierType.Multiplicative
            });
        }

        public string GetDescription() => EffectDescription;
        public IGemstoneEffect Copy() => new IncreaseBarricadeHealthEffect(this);
    }
    
    [Serializable]
    public class IncreaseBarricadeHealingEffect : IEffect, IGemstoneEffect
    {
        [OdinSerialize, Title("Extra Barricade Healing")]
        public float ModifierValue { get; set; }
        public string EffectDescription { get; set; }
        public Modifier.ModifierType CumulativeType => Modifier.ModifierType.Additive;
        public bool IsAdditivePercent => false;

        public IncreaseBarricadeHealingEffect() { }
        public IncreaseBarricadeHealingEffect(IncreaseBarricadeHealingEffect copy)
        {
            EffectDescription = copy.EffectDescription;
            ModifierValue = copy.ModifierValue;
        }
        
        public void Perform(IAttacker attacker) => PerformEffect();

        public void PerformEffect()
        {
            GameData.BarricadeHealing.AddModifier(new Modifier
            {
                Value = ModifierValue,
                Type = Modifier.ModifierType.Additive
            });
        }

        public string GetDescription() => EffectDescription;
        public IGemstoneEffect Copy() => new IncreaseBarricadeHealingEffect(this);
    }
    
    #endregion
    
    [Serializable]
    public class EnemySpeedModifierEffect : IEffect, IGemstoneEffect
    {
        [OdinSerialize, Title("Enemy Speed Multiplier")]
        public float ModifierValue { get; set; }
        public string EffectDescription { get; set; }
        public Modifier.ModifierType CumulativeType => Modifier.ModifierType.Multiplicative;
        public bool IsAdditivePercent => false;

        public EnemySpeedModifierEffect() { }
        public EnemySpeedModifierEffect(EnemySpeedModifierEffect copy)
        {
            EffectDescription = copy.EffectDescription;
            ModifierValue = copy.ModifierValue;
        }
        
        public void Perform(IAttacker attacker) => PerformEffect();

        public void PerformEffect()
        {
            GameDataManager.Instance.IncreaseGameData(new EnemySpeedModifierComponent
            {
                SpeedMultiplier = ModifierValue
            });
        }

        public string GetDescription() => EffectDescription;
        public IGemstoneEffect Copy() => new EnemySpeedModifierEffect(this);
    }
    
    [Serializable]
    public class EnemyDamageModifierEffect : IEffect, IGemstoneEffect
    {
        [OdinSerialize, Title("Enemy Damage Multiplier")]
        public float ModifierValue { get; set; }
        public string EffectDescription { get; set; }
        public Modifier.ModifierType CumulativeType => Modifier.ModifierType.Multiplicative;
        public bool IsAdditivePercent => false;

        public EnemyDamageModifierEffect() { }
        public EnemyDamageModifierEffect(EnemyDamageModifierEffect copy)
        {
            EffectDescription = copy.EffectDescription;
            ModifierValue = copy.ModifierValue;
        }
        
        public void Perform(IAttacker attacker) => PerformEffect();

        public void PerformEffect()
        {
            GameDataManager.Instance.IncreaseGameData(new EnemyDamageModifierComponent
            {
                DamageMultiplier = ModifierValue
            });
        }

        public string GetDescription() => EffectDescription;
        public IGemstoneEffect Copy() => new EnemyDamageModifierEffect(this);
    }
}
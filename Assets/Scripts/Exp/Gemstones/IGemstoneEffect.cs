using Gameplay.Upgrades;
using UnityEngine;
using Effects;

namespace Exp.Gemstones
{
    public interface IGemstoneEffect
    {
        public void PerformEffect();
        public string GetDescription();
    }
    
    public interface IGemstoneAppliedEffect : IGemstoneEffect
    {
        public void PerformEffect(AppliedEffectsHandler handler);
    }
    
    public class StatIncreaseEffect : IGemstoneAppliedEffect
    {
        public CategoryType AppliedCategory;
        public IEffect Effect;
        
        public string EffectDescription;
        public float Value;

        public void PerformEffect() => PerformEffect(Object.FindFirstObjectByType<AppliedEffectsHandler>());
        
        public void PerformEffect(AppliedEffectsHandler handler)
        {
            handler.AddUpgradeEffect(AppliedCategory, Effect);
        }
        
        public string GetDescription()
        {
            return EffectDescription;
        }
    }
}
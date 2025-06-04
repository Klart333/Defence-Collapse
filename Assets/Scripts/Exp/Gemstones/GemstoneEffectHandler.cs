using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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

            foreach (Gemstone activeGemstone in expManager.ActiveGemstones)
            {
                ActivateGemstoneEffect(activeGemstone);
            }
        }

        private void ActivateGemstoneEffect(Gemstone gem)
        {
            foreach (IGemstoneEffect effect in gem.Effects)   
            {
                if (effect is IGemstoneAppliedEffect appliedEffect)
                {
                    appliedEffect.PerformEffect(appliedEffectsHandler);
                    continue;
                }
                
                effect.PerformEffect();
            }
        }
    }
}
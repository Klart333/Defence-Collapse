using System.Collections.Generic;
using Sirenix.OdinInspector;
using Gameplay.Event;
using UnityEngine;
using Effects.UI;
using Loot;

namespace Effects
{
    public class EffectsHandler : MonoBehaviour
    {
        [Title("References")]
        [SerializeField]
        private UIEffectContainer mainEffectContainer;

        [SerializeField]
        private int containerAmount = 32;
        
        private HashSet<EffectModifier> availableEffects = new HashSet<EffectModifier>();
        
        private void OnEnable()
        {
            mainEffectContainer.Setup(containerAmount);
            
            Events.OnEffectGained += OnEffectGained;
            mainEffectContainer.OnEffectRemoved += OnEffectRemovedContainer;
        }

        private void OnDisable()
        {
            Events.OnEffectGained -= OnEffectGained;
            mainEffectContainer.OnEffectRemoved -= OnEffectRemovedContainer;
        }

        private void OnEffectGained(EffectModifier effect)
        {
            availableEffects.Add(effect);
            mainEffectContainer.SpawnEffect(effect);
        }
        
        private void OnEffectRemovedContainer(EffectModifier effect, int containerIndex)
        {
            availableEffects.Remove(effect);
        }
    }
}
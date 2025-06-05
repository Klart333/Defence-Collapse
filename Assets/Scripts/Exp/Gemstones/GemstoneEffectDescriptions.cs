using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Exp.Gemstones
{
    [CreateAssetMenu(fileName = "Gemstone Effect Descriptions", menuName = "Exp/Gemstone/Gemstone Effect Descriptions", order = 0)]
    public class GemstoneEffectDescriptions : SerializedScriptableObject
    {
        [SerializeField]
        private Dictionary<Type, EffectDescription> effectDescriptions = new Dictionary<Type, EffectDescription>();

        public string GetDescription(Type type, float value)
        {
            if (!effectDescriptions.TryGetValue(type, out EffectDescription effectDescription))
            {
                return "";
            }
            
            return string.Format(effectDescription.Description, value.ToString(effectDescription.IsPercentage ? "P" : "N"));
        }

        [Serializable]
        private struct EffectDescription
        {
            public string Description;
            public bool IsPercentage;
        }
    }
}
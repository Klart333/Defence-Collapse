using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Effects;
using System;

namespace Utility
{
    [InlineEditor, CreateAssetMenu(fileName = "Stat Color Utility", menuName = "Stat/Stat Color Utility", order = 0)]
    public class StatColorUtility : SerializedScriptableObject
    {
        [Title("Color Map")]
        [SerializeField]
        private Dictionary<StatTypeType, Color> colorMap = new Dictionary<StatTypeType, Color>();

        public Color GetColor(Type statType)
        {
            if (colorMap.TryGetValue(statType, out Color color))
            {
                return color;
            }
            
            return Color.white;
        }
    }
}
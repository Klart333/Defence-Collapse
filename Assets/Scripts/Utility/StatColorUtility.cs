using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Utility
{
    [InlineEditor, CreateAssetMenu(fileName = "Stat Color Utility", menuName = "Stat/Stat Color Utility", order = 0)]
    public class StatColorUtility : SerializedScriptableObject
    {
        [Title("Color Map")]
        [SerializeField]
        private Dictionary<StatType, Color> colorMap = new Dictionary<StatType, Color>();

        public Color GetColor(StatType statType)
        {
            if (colorMap.TryGetValue(statType, out Color color))
            {
                return color;
            }
            
            return Color.white;
        }
    }
}
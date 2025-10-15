using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using Variables;
using Effects;
using System;

namespace Utility
{
    [InlineEditor, CreateAssetMenu(fileName = "Stat Color Utility", menuName = "Stat/Stat Color Utility", order = 0)]
    public class StatColorUtility : SerializedScriptableObject
    {
        [Title("Color Map")]
        [SerializeField, OdinSerialize]
        private Dictionary<StatType, ColorReference> colorMap = new Dictionary<StatType, ColorReference>();

        public Color GetColor(Type statType)
        {
            if (colorMap.TryGetValue(new StatType(statType), out ColorReference color))
            {
                return color.Value;
            }
            
            return Color.white;
        }
        
#if UNITY_EDITOR

        [Button]
        public void ResetDictionary()
        {
            colorMap = new Dictionary<StatType, ColorReference>();

            ValueDropdownList<Type> list = new ValueDropdownList<Type>();
            foreach (Type t in typeof(Stat).Assembly.GetTypes())
            {
                if (!t.IsSubclassOf(typeof(Stat)) || t.IsAbstract) continue;
                
                colorMap.Add(new StatType(t), new ColorReference(Color.white));
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
        }
        
#endif
    }
}
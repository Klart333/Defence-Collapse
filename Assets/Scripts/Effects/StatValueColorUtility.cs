using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Variables;

namespace Effects
{
    [CreateAssetMenu(fileName = "Stat Value Color Utility", menuName = "Stat/Stat Value Color Utility", order = 0)]
    public class StatValueColorUtility : SerializedScriptableObject
    {
        [Title("Setup")]
        [SerializeField]
        private Dictionary<StatType, StatValueColor> statValueColors = new Dictionary<StatType, StatValueColor>();

        public Color GetColor(Type statType, float value)
        {
            if (!statValueColors.TryGetValue(new StatType(statType), out StatValueColor statValueColor))
            {
                Debug.LogError($"Stat type {statType} not found in dictionary");
                return Color.white;
            }

            for (int i = 1; i < statValueColor.ValueColors.Length; i++)
            {
                if (value < statValueColor.ValueColors[i].Value)
                {
                    return statValueColor.ValueColors[i - 1].Color.Value;
                }
            }

            return statValueColor.ValueColors[^1].Color.Value;
        }
        
        
#if UNITY_EDITOR

        [Button]
        public void ResetDictionary()
        {
            statValueColors = new Dictionary<StatType, StatValueColor>();

            foreach (Type t in typeof(Stat).Assembly.GetTypes())
            {
                if (!t.IsSubclassOf(typeof(Stat)) || t.IsAbstract) continue;

                statValueColors.Add(new StatType(t), new StatValueColor
                {
                    ValueColors = new[]
                    {
                        new ValueColor
                        {
                            Color = new ColorReference(Color.white)
                        }
                    }
                });
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
        }
        
#endif
    }

    [System.Serializable]
    public struct StatValueColor
    {
        public ValueColor[] ValueColors;
    }

    [System.Serializable]
    public struct ValueColor
    {
        public float Value;
        public ColorReference Color;
    }
}
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Variables;

namespace Effects
{
    [CreateAssetMenu(fileName = "Stat Icon Utility", menuName = "Stat/Stat Icon Utility", order = 0)]
    public class StatIconUtility : SerializedScriptableObject
    {
        [Title("Setup")]
        [SerializeField]
        private Dictionary<StatType, SpriteReference> statIcons = new Dictionary<StatType, SpriteReference>();

        public SpriteVariable GetIconVariable(Type statType)
        {
            if (!statIcons.TryGetValue(new StatType(statType), out SpriteReference icon))
            {
                Debug.LogError($"Stat type {statType} not found in dictionary", this);
                return null;
            }

            if (icon.Mode != ReferenceMode.Variable)
            {
                Debug.LogError($"Icon value is not a variable", this);
                return null;
            }
            
            return icon.Variable;
        }
        
        public Sprite GetIcon(Type statType)
        {
            if (!statIcons.TryGetValue(new StatType(statType), out SpriteReference icon))
            {
                Debug.LogError($"Stat type {statType} not found in dictionary");
                return null;
            }

            return icon.Value;
        }
        
        
#if UNITY_EDITOR

        [Button]
        public void ResetDictionary()
        {
            statIcons = new Dictionary<StatType, SpriteReference>();

            foreach (Type t in typeof(Stat).Assembly.GetTypes())
            {
                if (!t.IsSubclassOf(typeof(Stat)) || t.IsAbstract) continue;
                
                statIcons.Add(new StatType(t), new SpriteReference());
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
        }
        
#endif
    }
}
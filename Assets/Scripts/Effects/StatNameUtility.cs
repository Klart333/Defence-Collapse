using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using Variables;
using System;

namespace Effects
{
    [InlineEditor, CreateAssetMenu(fileName = "Stat Name Utility", menuName = "Stat/Stat Name Utility", order = 0)]
    public class StatNameUtility : SerializedScriptableObject
    {
        [Title("Names")]
        [SerializeField, OdinSerialize]
        private Dictionary<StatType, StringReference> statNames = new Dictionary<StatType, StringReference>();

        [Title("Descriptions")]
        [SerializeField, OdinSerialize]
        private Dictionary<StatType, StatDescription> descriptions = new Dictionary<StatType, StatDescription>();
        
        public string GetStatName(Type statType) => statNames[new StatType(statType)].Value;

        public string GetDescription(Type statType, float value)
        {
            StatDescription statDescription = descriptions[new StatType(statType)];
            return string.Format(statDescription.Description.Value, value.ToString(statDescription.IsPercentage ? "P" : "N"));
        }

#if UNITY_EDITOR

        [Button]
        public void ResetDictionaries()
        {
            statNames = new Dictionary<StatType, StringReference>();
            descriptions = new Dictionary<StatType, StatDescription>();

            foreach (Type t in typeof(Stat).Assembly.GetTypes())
            {
                if (!t.IsSubclassOf(typeof(Stat)) || t.IsAbstract) continue;
                
                statNames.Add(new StatType(t), new StringReference(t.Name.Substring(0, t.Name.Length - 4)));
                descriptions.Add(new StatType(t), new StatDescription
                {
                    Description = new StringReference("{0}"),
                    IsPercentage = false,
                });
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(this); 
        }
        
#endif
        
        [Serializable]
        private struct StatDescription
        {
            public StringReference Description;
            public bool IsPercentage;
        }
    }
}
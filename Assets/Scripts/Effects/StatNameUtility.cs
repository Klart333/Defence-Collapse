using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Effects
{
    [InlineEditor, CreateAssetMenu(fileName = "Stat Name Utility", menuName = "Stat/Stat Name Utility", order = 0)]
    public class StatNameUtility : SerializedScriptableObject
    {
        [Title("Names")]
        [SerializeField]
        private Dictionary<StatType, string> statNames = new Dictionary<StatType, string>();
        
        [Title("Descriptions")]
        [SerializeField]
        private Dictionary<StatType, StatDescription> descriptions = new Dictionary<StatType, StatDescription>();
        
        public string GetStatName(StatType statType) => statNames[statType];

        public string GetDescription(StatType statType, float value)
        {
            StatDescription statDescription = descriptions[statType];
            return string.Format(statDescription.Description, value.ToString(statDescription.IsPercentage ? "P" : "N"));
        } 
        
    
        [Serializable]
        private struct StatDescription
        {
            public string Description;
            public bool IsPercentage;
        }
    }
}
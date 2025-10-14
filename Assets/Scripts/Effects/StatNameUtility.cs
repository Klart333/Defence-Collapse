using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using Variables;

namespace Effects
{
    [InlineEditor, CreateAssetMenu(fileName = "Stat Name Utility", menuName = "Stat/Stat Name Utility", order = 0)]
    public class StatNameUtility : SerializedScriptableObject
    {
        [Title("Names")]
        [SerializeField, OdinSerialize]
        private Dictionary<StatTypeType, StringReference> statNames = new Dictionary<StatTypeType, StringReference>();
        
        [Title("Descriptions")]
        [SerializeField]
        private Dictionary<StatTypeType, StatDescription> descriptions = new Dictionary<StatTypeType, StatDescription>();
        
        public string GetStatName(Type statType) => statNames[statType].Value;

        public string GetDescription(Type statType, float value)
        {
            StatDescription statDescription = descriptions[statType];
            return string.Format(statDescription.Description.Value, value.ToString(statDescription.IsPercentage ? "P" : "N"));
        } 
        
        [Serializable]
        private struct StatDescription
        {
            public StringReference Description;
            public bool IsPercentage;
        }
    }
}
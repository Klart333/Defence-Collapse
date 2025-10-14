using System;
using Sirenix.OdinInspector;
using UnityEngine;
using Effects;
using Utility;
using TMPro;

namespace Buildings.District.UI
{
    public class UIStatPanel : PooledMonoBehaviour
    {
        [Title("References")]
        [SerializeField]
        private TextMeshProUGUI statNameText;
        
        [SerializeField]
        private TextMeshProUGUI statValueText;

        [SerializeField]
        private StatNameUtility statNameUtility;
        
        [SerializeField]
        private StatColorUtility statColorUtility;
        
        private Type statType;
        
        public Stat Stat { get; private set; }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (Stat != null)
            {
                Stat.OnValueChanged -= OnStatChanged;
                Stat = null;
            }
        }

        public void DisplayStat(Stat stat)
        {
            Stat = stat;
            statType = stat.GetType();
            
            statNameText.text = statNameUtility.GetStatName(statType);
            statNameText.color = statColorUtility.GetColor(statType);
            statValueText.text = statNameUtility.GetDescription(statType, Stat.Value);
            
            Stat.OnValueChanged += OnStatChanged;
        }

        private void OnStatChanged()
        {
            statValueText.text = statNameUtility.GetDescription(statType, Stat.Value);
        }
    }
}
using Sirenix.OdinInspector;
using UnityEngine;
using Variables;
using Effects;
using System;
using TMPro;

namespace Buildings.District.UI
{
    public class UIStatDisplay : PooledMonoBehaviour
    {
        [Title("Setup")]
        [SerializeField]
        private TextMeshProUGUI statText;
        
        [Title("References")]
        [SerializeField]
        private StatIconUtility statIconUtility;
        
        [SerializeField]
        private StatValueColorUtility statValueColorUtility;
        
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
            
            SetStatText(stat);

            Stat.OnValueChanged += OnStatChanged;
        }

        private void SetStatText(Stat stat)
        {
            SpriteVariable spriteVariable = statIconUtility.GetIconVariable(statType);
            string valueColor = ColorUtility.ToHtmlStringRGB(statValueColorUtility.GetColor(statType, stat.Value));
            bool isPercentage = stat is IPercentageStat;
            string statValue = isPercentage ? $"{stat.Value:P}" : $"{stat.Value:N}";
            statText.text = $"{spriteVariable.ToTag()}   <color=#{valueColor}>{statValue}</color>";
        }

        private void OnStatChanged()
        {
            SetStatText(Stat);
        }
    }
}
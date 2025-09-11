using Effects;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using Utility;

namespace Buildings.District
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
        
        public StatType StatType { get; private set; }
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

        public void DisplayStat(Stat stat, StatType type)
        {
            Stat = stat;
            StatType = type; 
            
            statNameText.text = statNameUtility.GetStatName(StatType);
            statNameText.color = statColorUtility.GetColor(StatType);
            statValueText.text = statNameUtility.GetDescription(StatType, Stat.Value);
            
            Stat.OnValueChanged += OnStatChanged;
        }

        private void OnStatChanged()
        {
            statValueText.text = statNameUtility.GetDescription(StatType, Stat.Value);
        }
    }
}
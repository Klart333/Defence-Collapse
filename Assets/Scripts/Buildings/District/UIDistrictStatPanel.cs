using System.Collections.Generic;
using Effects;
using Sirenix.OdinInspector;
using UnityEngine.UI;
using UnityEngine;
using Health;
using TMPro;

namespace Buildings.District
{
    public class UIDistrictStatPanel : SerializedMonoBehaviour
    {
        [Title("References")]
        [SerializeField]
        private TextMeshProUGUI displayableNameText;

        [Title("References", "Health")]
        [SerializeField]
        private GameObject healthParent;
        
        [SerializeField]
        private TextMeshProUGUI healthText;

        [SerializeField]
        private Image healthFillImage;
        
        [Title("References", "Stats")]
        [SerializeField]
        private UIStatPanel statPanelPrefab;
        
        [SerializeField]
        private Transform statPanelParent;

        [SerializeField]
        private Dictionary<StatDisplayableType, StatType[]> statTypes = new Dictionary<StatDisplayableType, StatType[]>();
        
        private List<UIStatPanel> spawnedStatPanels = new List<UIStatPanel>();

        private void OnDisable()
        {
            foreach (UIStatPanel panel in spawnedStatPanels)
            {
                panel.gameObject.SetActive(false);
            }
            
            spawnedStatPanels.Clear();
        }

        public void DisplayStats(StatDisplayableType displayableType, string name, Stats stats, HealthComponent health = null)
        {
            displayableNameText.text = name;

            bool hasHealth = health != null;
            healthParent.SetActive(hasHealth);
            if (hasHealth)
            {
                healthText.text = $"{health.CurrentHealth} / {health.MaxHealth}";
                healthFillImage.fillAmount = health.HealthPercentage;
            }
            
            StatType[] statsToDisplay = statTypes[displayableType];
            for (int i = 0; i < statsToDisplay.Length; i++)
            {
                StatType statType = statsToDisplay[i];
                UIStatPanel spawned = statPanelPrefab.GetDisabled<UIStatPanel>();
                spawned.transform.SetParent(statPanelParent, false);
                spawned.transform.SetSiblingIndex(i);
                Stat stat = stats.Get(statType);
                spawned.DisplayStat(stat, statType);
                spawned.gameObject.SetActive(true);
                spawnedStatPanels.Add(spawned);
            }
        }
    }

    public enum StatDisplayableType
    {
        District,
        Wall,
        Barricade,
    }
}
using System;
using System.Collections.Generic;
using Buildings.District.UI;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Effects
{
    public class UIStatGroupDisplay : PooledMonoBehaviour
    {
        [Title("Setup", "Stats")]
        [SerializeField]
        private UIStatDisplay statDisplayPrefab;
        
        [SerializeField]
        private Transform statPanelParent;

        private List<UIStatDisplay> spawnedStatPanels = new List<UIStatDisplay>();
        
        protected override void OnDisable()
        {
            foreach (UIStatDisplay panel in spawnedStatPanels)
            {
                panel.gameObject.SetActive(false);
            }
            
            spawnedStatPanels.Clear();
        }
        
        public void DisplayStats(Type[] statTypes, Stats stats)
        {
            for (int i = 0; i < statTypes.Length; i++)
            {
                UIStatDisplay spawned = statDisplayPrefab.GetDisabled<UIStatDisplay>();
                spawned.transform.SetParent(statPanelParent, false);
                spawned.transform.SetSiblingIndex(i);
                Stat stat = stats.Get(statTypes[i]);
                spawned.DisplayStat(stat);
                spawned.gameObject.SetActive(true);
                spawnedStatPanels.Add(spawned);
            }
        }
    }
}
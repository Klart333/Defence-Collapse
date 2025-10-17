using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Effects;
using System;

namespace Buildings.District.UI
{
    public class UIDistrictStatPanel : MonoBehaviour
    {
        [Title("Setup")]
        [SerializeField]
        private UIStatGroupDisplay statGroupDisplayPrefab;
        
        [SerializeField]
        private Transform statGroupContainer;
        
        private List<UIStatGroupDisplay> spawnedGroups = new List<UIStatGroupDisplay>();
        
        private List<Type> groupTypes = new List<Type>
        {
            typeof(AttackDistrictStats),
            typeof(ProductionDistrictStats),
            typeof(EffectsDistrictStats),
        };

        private void OnDisable()
        {
            foreach (UIStatGroupDisplay panel in spawnedGroups)
            {
                panel.gameObject.SetActive(false);
            }
            
            spawnedGroups.Clear();
        }

        public void DisplayStats(Stats stats)
        {
            for (int i = 0; i < groupTypes.Count; i++)
            {
                Type[] statTypes = StatUtility.StatTypes[groupTypes[i]];
                
                UIStatGroupDisplay spawned = statGroupDisplayPrefab.GetDisabled<UIStatGroupDisplay>();
                spawned.transform.SetParent(statGroupContainer, false);
                spawned.transform.SetSiblingIndex(i);
                spawned.DisplayStats(statTypes, stats);
                spawned.gameObject.SetActive(true);
                
                spawnedGroups.Add(spawned);
            }
        }
    }
}
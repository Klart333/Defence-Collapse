using System;
using Gameplay.Event;
using Sirenix.OdinInspector;
using UI;
using UnityEngine;
using UnityEngine.Serialization;

namespace Buildings.District.UI
{
    public class UIDistrictUnlockHandler : MonoBehaviour
    {
        public event Action<TowerData, UIDistrictButton> OnDistrictButtonSpawned;
        
        [Title("Settings")]
        [SerializeField]
        private UIDistrictButton districtButtonPrefab;
        
        [SerializeField]
        private Transform districtContainer;
        
        [Title("Start up")]
        [SerializeField]
        private TowerData startingTowerData;
        
        private DistrictHandler districtHandler; 
            
        private void OnEnable()
        {
            districtHandler = FindFirstObjectByType<DistrictHandler>();
            
            Events.OnDistrictUnlocked += OnDistrictUnlocked;
            Events.OnDistrictBuilt += OnDistrictBuilt;
        }

        private void OnDisable()
        {
            Events.OnDistrictUnlocked -= OnDistrictUnlocked;
            Events.OnDistrictBuilt -= OnDistrictBuilt;
        }

        private void OnDistrictBuilt(TowerData towerData)
        {
            if (towerData.DistrictType is not DistrictType.TownHall)
            {
                return;
            }
            
            Events.OnDistrictBuilt -= OnDistrictBuilt;

            OnDistrictUnlocked(startingTowerData);
        }
        
        private void OnDistrictUnlocked(TowerData towerData)
        {
            UIDistrictButton districtButton = Instantiate(districtButtonPrefab, districtContainer);
            districtButton.Setup(districtHandler, towerData);
            districtButton.transform.SetSiblingIndex(2);
            
            OnDistrictButtonSpawned?.Invoke(towerData, districtButton);
        }
    }
}
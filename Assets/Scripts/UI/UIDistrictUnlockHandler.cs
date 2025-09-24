using System;
using Buildings.District;
using Gameplay.Event;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace UI
{
    public class UIDistrictUnlockHandler : MonoBehaviour
    {
        public event Action<TowerData, UIDistrictButton> OnDistrictButtonSpawned;
        
        [FormerlySerializedAs("districtFoldout")]
        [Title("Settings")]
        [SerializeField]
        private UIFoldoutController foldoutController;

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
            
            OnDistrictUnlocked(startingTowerData);
        }

        private void OnDisable()
        {
            Events.OnDistrictUnlocked -= OnDistrictUnlocked;
        }

        private void OnDistrictUnlocked(TowerData towerData)
        {
            UIDistrictButton districtButton = Instantiate(districtButtonPrefab, districtContainer);
            districtButton.Setup(districtHandler, towerData);
            districtButton.transform.SetSiblingIndex(2);
            
            OnDistrictButtonSpawned?.Invoke(towerData, districtButton);

            if (foldoutController.IsOpen)
            {
                foldoutController.ToggleOpen(true);
            }
        }
    }
}
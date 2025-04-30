using System.Collections.Generic;
using UnityEngine;
using Gameplay;
using UI;

namespace Buildings.District
{
    public class DistrictUnlockHandler : MonoBehaviour
    {
        [SerializeField]
        private TowerData[] unlockableTowers;
        
        [SerializeField]
        private UIDistrictUnlockPanel[] unlockPanels;
        
        [SerializeField]
        private GameObject canvasGameObject;
        
        private void OnEnable()
        {
            Events.OnDistrictBuilt += OnDistrictBuilt;
        }

        private void OnDisable()
        {
            Events.OnDistrictBuilt -= OnDistrictBuilt;
        }

        private void OnDistrictBuilt(DistrictType districtType)
        {
            if (districtType != DistrictType.TownHall) return;

            canvasGameObject.SetActive(true);
            List<TowerData> towers = new List<TowerData>(unlockableTowers);
            for (int i = 0; i < unlockPanels.Length; i++)
            {
                TowerData towerData = towers[Random.Range(0, towers.Count)];
                unlockPanels[i].DisplayDistrict(towerData, ChoseDistrict);
            }
            
            GameSpeedManager.Instance.SetGameSpeed(0, 1.0f);
        }

        public void ChoseDistrict(TowerData tower)
        {
            GameSpeedManager.Instance.SetGameSpeed(1, 1.0f);
            
            canvasGameObject.SetActive(false);
        }
    }
}
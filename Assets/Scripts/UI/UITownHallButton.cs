using System;
using Buildings.District;
using Gameplay.Event;
using UnityEngine;
using UnityEngine.Serialization;

namespace UI
{
    public class UITownHallButton : MonoBehaviour
    {
        [SerializeField]
        private GameObject[] disabledFoldoutObjects;
        
        [SerializeField]
        private GameObject townHallButton;
        
        [SerializeField]
        private TowerData townHallData;

        [FormerlySerializedAs("townHallFlipButton")]
        [SerializeField]
        private UIDistrictToggleButton townHallToggleButton;
        
        private void Awake()
        {
            townHallButton.SetActive(true);
            for (int i = 0; i < disabledFoldoutObjects.Length; i++)
            {
                disabledFoldoutObjects[i].SetActive(false);
            }
        }

        private void OnEnable()
        {
            Events.OnDistrictBuilt += OnDistrictBuilt;
            townHallToggleButton.OnClick += OnClick;
        }

        private void OnDisable()
        {
            Events.OnDistrictBuilt -= OnDistrictBuilt;
            townHallToggleButton.OnClick -= OnClick;
        }

        private void OnDistrictBuilt(TowerData towerData)
        {
            if (towerData.DistrictType is not DistrictType.TownHall)
            {
                return;
            }
            
            Events.OnDistrictBuilt -= OnDistrictBuilt;

            townHallButton.SetActive(false);
            for (int i = 0; i < disabledFoldoutObjects.Length; i++)
            {
                disabledFoldoutObjects[i].SetActive(true);
            }
        }

        public void OnClick()
        {
            Events.OnDistrictClicked?.Invoke(townHallData);
        }
    }
}
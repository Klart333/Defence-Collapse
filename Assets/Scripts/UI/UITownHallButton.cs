using System;
using UnityEngine;

namespace UI
{
    public class UITownHallButton : MonoBehaviour
    {
        [SerializeField]
        private GameObject[] disabledFoldoutObjects;
        
        [SerializeField]
        private GameObject townHallButton;

        [SerializeField]
        private UIDistrictFlipButton townHallFlipButton;
        
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
            townHallFlipButton.OnClick += OnClick;
        }

        private void OnDisable()
        {
            Events.OnDistrictBuilt -= OnDistrictBuilt;
            townHallFlipButton.OnClick -= OnClick;
        }

        private void OnDistrictBuilt(DistrictType type)
        {
            if (type is not DistrictType.TownHall)
            {
                return;
            }
            
            townHallButton.SetActive(false);
            for (int i = 0; i < disabledFoldoutObjects.Length; i++)
            {
                disabledFoldoutObjects[i].SetActive(true);
            }
        }

        public void OnClick()
        {
            Events.OnDistrictClicked?.Invoke(DistrictType.TownHall, 2);
        }
    }
}
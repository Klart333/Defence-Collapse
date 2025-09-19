using System;
using System.Collections.Generic;
using Buildings.District;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Enemy.ECS;
using Gameplay;
using InputCamera;
using Pathfinding;
using Sirenix.OdinInspector;

namespace UI
{
    public class UIDistrictDisplayHandler : MonoBehaviour 
    {
        [Title("Setup")]
        [SerializeField]
        private UIDistrictDisplay displayPrefab;

        [SerializeField]
        private RectTransform displayParent;
     
        [Title("References")]
        [SerializeField]
        private DistrictHandler districtHandler;
        
        private List<UIDistrictDisplay> spawnedDisplays = new List<UIDistrictDisplay>();

        private void OnEnable()
        {
            districtHandler.OnDistrictDisplayed += Display;
        }

        private void OnDisable()
        {
            districtHandler.OnDistrictDisplayed -= Display;
        }

        private void Display(DistrictData districtData)
        {
            UIDistrictDisplay display = displayPrefab.Get<UIDistrictDisplay>(displayParent);
            display.Display(districtData);
            spawnedDisplays.Add(display);
        }

        private void HideDisplays()
        {
            for (int i = 0; i < spawnedDisplays.Count; i++)
            {
                spawnedDisplays[i].Hide();
            }
            
            spawnedDisplays.Clear();
        }
    }
}
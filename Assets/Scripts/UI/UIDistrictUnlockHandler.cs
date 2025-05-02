using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UI
{
    public class UIDistrictUnlockHandler : SerializedMonoBehaviour
    {
        [SerializeField]
        private Dictionary<TowerData, Transform> towerDataDictionary = new Dictionary<TowerData, Transform>();

        [SerializeField]
        private UIDistrictFoldout districtFoldout;

        private void OnEnable()
        {
            Events.OnDistrictUnlocked += OnDistrictUnlocked;
        }

        private void OnDisable()
        {
            Events.OnDistrictUnlocked -= OnDistrictUnlocked;
        }

        private void OnDistrictUnlocked(TowerData towerData)
        {
            towerDataDictionary[towerData].SetSiblingIndex(1);
            towerDataDictionary[towerData].gameObject.SetActive(true);
            if (districtFoldout.IsOpen)
            {
                districtFoldout.ToggleOpen(true);
            }
        }
    }
}
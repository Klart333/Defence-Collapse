using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Buildings.District
{
    [InlineEditor, CreateAssetMenu(fileName = "Tower Data Utility", menuName = "Building/Tower Data Utility", order = 0)]
    public class TowerDataUtility : SerializedScriptableObject
    {
        [Title("Tower Data Utility")]
        [OdinSerialize]
        private Dictionary<DistrictType, TowerData> towerDataDictionay = new Dictionary<DistrictType, TowerData>();

        public TowerData GetTowerData(DistrictType districtType)
        {
            if (towerDataDictionay.TryGetValue(districtType, out TowerData towerData))
            {
                return towerData;
            }
            
            Debug.LogError($"District {districtType} not found in TowerDataUtility");
            return null;
        }
    }
}
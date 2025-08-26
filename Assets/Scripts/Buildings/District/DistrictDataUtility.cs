using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Buildings.District
{
    [CreateAssetMenu(fileName = "District Data Utility", menuName = "District/Utility/District Data Utility", order = 0)]
    public class DistrictDataUtility : SerializedScriptableObject
    {
        [SerializeField]
        private Dictionary<DistrictType, TowerData> districtDatas = new Dictionary<DistrictType, TowerData>();

        public TowerData GetTowerData(DistrictType districtType)
        {
            if (districtDatas.TryGetValue(districtType, out TowerData towerData))
            {
                return towerData;
            }
            
            Debug.LogError("District Data for type: " + districtType + " not found");
            return null;
        }
    }
}
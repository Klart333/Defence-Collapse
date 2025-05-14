using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using System;

namespace Gameplay
{
    [CreateAssetMenu(fileName = "DistrictCostUtility", menuName = "District/DistrictCostUtility", order = 0), InlineEditor]
    public class DistrictCostUtility : SerializedScriptableObject
    {
        [SerializeField]
        private Dictionary<DistrictType, CostData> districtPowerBases = new Dictionary<DistrictType, CostData>();
        
        public float GetCost(DistrictType districtType, int districtAmount)
        {
            CostData costData = districtPowerBases[districtType];
            return costData.Base + costData.Increase * districtAmount;
        }

        [Serializable]
        private struct CostData
        {
            public float Base;
            public float Increase;
        } 
    }
}
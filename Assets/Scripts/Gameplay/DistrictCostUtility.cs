using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay
{
    [CreateAssetMenu(fileName = "DistrictCostUtility", menuName = "District/DistrictCostUtility", order = 0), InlineEditor]
    public class DistrictCostUtility : SerializedScriptableObject
    {
        [SerializeField]
        private Dictionary<DistrictType, int> districtPowerBases = new Dictionary<DistrictType, int>();
        
        public float GetCost(DistrictType districtType, int chunkAmount)
        {
            int basePower = districtPowerBases[districtType];
            return 4 * Mathf.Pow(basePower, Mathf.Sqrt(chunkAmount) - 2); // https://www.desmos.com/calculator
        }
    }
}
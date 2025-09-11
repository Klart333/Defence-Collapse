using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using WaveFunctionCollapse;

namespace Buildings.District
{
    [CreateAssetMenu(fileName = "District Prototype Info Utility", menuName = "District/Utility/Prototype Info Utility", order = 0)]
    public class DistrictPrototypeInfoUtility : SerializedScriptableObject
    {
        [OdinSerialize]
        private Dictionary<DistrictType, PrototypeInfoData> districtInfoData = new Dictionary<DistrictType, PrototypeInfoData>();

        public PrototypeInfoData GetPrototypeInfo(DistrictType districtType)
        {
            if (districtInfoData.TryGetValue(districtType, out PrototypeInfoData result))
            {
                return result;
            }
            
            Debug.LogError("District Prototype Info Utility: District Type not found", this);
            return null;
        }
    }
}
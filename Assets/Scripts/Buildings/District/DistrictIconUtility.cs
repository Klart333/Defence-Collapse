using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Variables;

namespace Buildings.District
{
    [InlineEditor, CreateAssetMenu(fileName = "District Icon Utility", menuName = "District/Utility/Icon", order = 0)]
    public class DistrictIconUtility : SerializedScriptableObject
    {
        [SerializeField]
        private Dictionary<DistrictType, SpriteReference> icons = new Dictionary<DistrictType, SpriteReference>();

        public SpriteReference GetIcon(DistrictType districtType)
        {
            if (icons.TryGetValue(districtType, out var sprite))
            {
                return sprite;
            }
            
            Debug.LogError($"Requested district type ({districtType}) did not have a icon");
            return null;
        }
    }
}
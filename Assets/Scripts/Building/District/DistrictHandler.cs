using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

namespace Buildings.District
{
    
    public class DistrictHandler : MonoBehaviour
    {
        // NEEDS TO KEEP TRACK OF WHICH CELLS ARE PART OF A DISTRICT, AND WHAT TYPE
        // CAN PROBABLY HANDLE THE COLLIDERS...
        // NEEDS A REFERENCE TO THE BUILDING GROUP INDEX, WHEN CLICKING ON THE CITY THE DISTRICTS SHOULD BE SHOWN, FUTURE THING THO
        // KEEP IT SIMPLE
        // NEEDS REFERENCE TO THE DISTRICT GENERATOR TO REGENERATE THE INDEXES INTO THE RIGHT TYPE
        // SHOULD LISTEN TO THE EVENT.CS AND TELL THE DISPLAY WHAT TO DISPLAY, OTHERWISE COULD QUERY BUT DOESN'T SOUND GOOD
        // MAKE THE THINGS SELECTABLE, AND IMPLEMENT MINIMUM SIZE PER TYPE

        [Title("District")]
        [SerializeField]
        private DistrictGenerator districtGenerator;
    
        private readonly Dictionary<int2, DistrictData> districts = new Dictionary<int2, DistrictData>();

        public void BuildDistrict(List<Cell> cells, DistrictType districtType)
        {
            Vector3 position = Vector3.zero; // Middle point of cells
            DistrictData buildingData = GetDistrictData(districtType, cells.Count, position);
        
            for (int i = 0; i < cells.Count; i++)
            {
            
            }
        }

        private DistrictData GetDistrictData(DistrictType districtType, int cellsCount, Vector3 position)
        {
            DistrictData districtData = new DistrictData(districtType, cellsCount, position);
            return districtData;
        }

        public bool IsBuilt(Cell cell)
        {
            int2 index = GetDistrictIndex(cell.Position);
            return districts.TryGetValue(index, out _);
        }

        public int2 GetDistrictIndex(Vector3 position)
        {
            int x = Math.GetMultiple(position.x, districtGenerator.ChunkWaveFunction.GridScale.x);
            int y = Math.GetMultiple(position.z, districtGenerator.ChunkWaveFunction.GridScale.z);
        
            return new int2(x, y);
        }
    }
}

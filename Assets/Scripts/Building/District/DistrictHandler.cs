using System;
using System.Collections.Generic;
using System.Linq;
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
            Vector3 position = Vector3.zero;
            for (int i = 0; i < cells.Count; i++)
            {
                position += cells[i].Position;
            }
            position /= cells.Count;
            
            DistrictData districtData = GetDistrictData(districtType, cells.Count, position);

            for (int i = 0; i < cells.Count; i++)
            {
                districts.Add(GetDistrictIndex(cells[i].Position), districtData);
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
            int x = Math.GetMultipleFloored(position.x, districtGenerator.ChunkWaveFunction.GridScale.x);
            int y = Math.GetMultipleFloored(position.z, districtGenerator.ChunkWaveFunction.GridScale.z);
            return new int2(x, y);
        }

        public static bool CanBuildDistrict(int width, int depth, DistrictType currentType)
        {
            return currentType switch
            {
                DistrictType.Archer => width >= 8 && depth >= 8,
                DistrictType.Bomb => width >= 12 && depth >= 12,
                DistrictType.Church => width >= 12 && depth >= 12,
                DistrictType.Farm => width >= 6 && depth >= 6,
                DistrictType.Mine => width >= 6 && depth >= 6,
                _ => throw new ArgumentOutOfRangeException(nameof(currentType), currentType, null)
            };
        }
    }
}

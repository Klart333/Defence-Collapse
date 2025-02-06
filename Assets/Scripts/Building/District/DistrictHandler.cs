using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
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

        public void BuildDistrict(List<Chunk> chunks, DistrictType districtType)
        {
            Vector3 position = Vector3.zero;
            for (int i = 0; i < chunks.Count; i++)
            {
                position += chunks[i].Position;
            }
            position /= chunks.Count;
            
            DistrictData districtData = GetDistrictData(districtType, chunks.Count, position);

            for (int i = 0; i < chunks.Count; i++)
            {
                districts.Add(GetDistrictIndex(chunks[i].Position, districtGenerator), districtData);
                
                chunks[i].Clear(districtGenerator.ChunkWaveFunction.GameObjectPool);
                districtGenerator.ChunkWaveFunction.LoadCells(chunks[i]);
            }
            
            districtGenerator.Run().Forget(Debug.LogError);
        }

        private DistrictData GetDistrictData(DistrictType districtType, int cellsCount, Vector3 position)
        {
            DistrictData districtData = new DistrictData(districtType, cellsCount, position);
            return districtData;
        }

        public bool IsBuilt(Chunk chunk)
        {
            int2 index = GetDistrictIndex(chunk.Position, districtGenerator);
            return districts.TryGetValue(index, out _);
        }

        public static int2 GetDistrictIndex(Vector3 position, IChunkWaveFunction wave)
        {
            int x = Math.GetMultipleFloored(position.x, wave.ChunkSize.x);
            int y = Math.GetMultipleFloored(position.z, wave.ChunkSize.z);
            return new int2(x, y);
        }   

        public static bool CanBuildDistrict(int width, int depth, DistrictType currentType)
        {
            return currentType switch
            {
                DistrictType.Archer => width >= 2 && depth >= 2,
                DistrictType.Bomb => width >= 3 && depth >= 3,
                DistrictType.Church => width >= 3 && depth >= 3,
                DistrictType.Farm => width >= 2 && depth >= 2,
                DistrictType.Mine => width >= 2 && depth >= 2,
                _ => throw new ArgumentOutOfRangeException(nameof(currentType), currentType, null)
            };
        }
    }
}

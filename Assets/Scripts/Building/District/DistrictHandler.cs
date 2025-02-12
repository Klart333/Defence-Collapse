using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Unity.Mathematics;
using UnityEngine;
using WaveFunctionCollapse;

namespace Buildings.District
{
    public class DistrictHandler : SerializedMonoBehaviour
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
        
        [OdinSerialize]
        private Dictionary<DistrictType, PrototypeInfoData> districtInfoData = new Dictionary<DistrictType, PrototypeInfoData>();
    
        private readonly Dictionary<int2, DistrictData> districts = new Dictionary<int2, DistrictData>();

        public void BuildDistrict(HashSet<Chunk> chunks, DistrictType districtType)
        {
            if (!districtInfoData.TryGetValue(districtType, out PrototypeInfoData prototypeInfo))
            {
                Debug.LogError("Could not find PrototypeInfoData for DistrictType: " + districtType);
                return;
            }
            
            Vector3 position = Vector3.zero;
            foreach (Chunk chunk in chunks)
            {
                position += chunk.Position;
            }
            position /= chunks.Count;
            
            DistrictData districtData = GetDistrictData(districtType, chunks.Count, position);

            HashSet<Chunk> neighbours = new HashSet<Chunk>();
            foreach (Chunk chunk in chunks)
            {
                GetNeighbours(chunks, chunk, neighbours, 1);
                
                districts.Add(chunk.ChunkIndex.xz, districtData);
                
                chunk.Clear(districtGenerator.ChunkWaveFunction.GameObjectPool);
                districtGenerator.ChunkWaveFunction.LoadCells(chunk, prototypeInfo);

                const int height = 2;
                for (int j = 0; j < height; j++)
                {
                    int heightLevel = 1 + j; // Assumes that the chunk has exactly 2 levels already
                    Vector3 pos = chunk.Position + Vector3.up * districtGenerator.ChunkScale.y * heightLevel; 
                    districtGenerator.ChunkWaveFunction.LoadChunk(pos, districtGenerator.ChunkSize, prototypeInfo);
                }
            }

            foreach (Chunk chunk in neighbours)
            {
                chunk.Clear(districtGenerator.ChunkWaveFunction.GameObjectPool);
                districtGenerator.ChunkWaveFunction.LoadCells(chunk, chunk.PrototypeInfoData);
            }
            
            districtGenerator.Run().Forget(Debug.LogError);
        }

        private static void GetNeighbours(HashSet<Chunk> chunks, Chunk chunk, HashSet<Chunk> neighbours, int depth)
        {
            foreach (Chunk adjacentChunk in chunk.AdjacentChunks)
            {
                if (adjacentChunk == null || chunks.Contains(adjacentChunk))
                {
                    continue;
                }
                    
                neighbours.Add(adjacentChunk);
                if (depth > 0)
                {
                    GetNeighbours(chunks, adjacentChunk, neighbours, depth - 1);
                }
            }
        }

        private DistrictData GetDistrictData(DistrictType districtType, int cellsCount, Vector3 position)
        {
            DistrictData districtData = new DistrictData(districtType, cellsCount, position);
            return districtData;
        }

        public bool IsBuilt(Chunk chunk)
        {
            int2 index = chunk.ChunkIndex.xz;
            return districts.TryGetValue(index, out _);
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

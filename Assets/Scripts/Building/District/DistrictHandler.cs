using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using WaveFunctionCollapse;
using Sirenix.Utilities;
using Unity.Mathematics;
using UnityEngine;
using System;

namespace Buildings.District
{
    public class DistrictHandler : SerializedMonoBehaviour
    {
        public event Action<DistrictData> OnDistrictCreated;
        
        // NEEDS TO KEEP TRACK OF WHICH CELLS ARE PART OF A DISTRICT, AND WHAT TYPE
        // CAN PROBABLY HANDLE THE COLLIDERS...
        // NEEDS A REFERENCE TO THE BUILDING GROUP INDEX, WHEN CLICKING ON THE CITY THE DISTRICTS SHOULD BE SHOWN, FUTURE THING THO

        [Title("District")]
        [SerializeField]
        private DistrictGenerator districtGenerator;
        
        [OdinSerialize]
        private Dictionary<DistrictType, PrototypeInfoData> districtInfoData = new Dictionary<DistrictType, PrototypeInfoData>();
    
        private readonly Dictionary<int2, DistrictData> districts = new Dictionary<int2, DistrictData>();
        private readonly List<DistrictData> uniqueDistricts = new List<DistrictData>();

        private int districtKey;

        private void Update()
        {
            for (int i = 0; i < uniqueDistricts.Count; i++)
            {
                uniqueDistricts[i].Update();
            }
        }

        public void BuildDistrict(HashSet<Chunk> chunks, DistrictType districtType)
        {
            if (!districtInfoData.TryGetValue(districtType, out PrototypeInfoData prototypeInfo))
            {
                Debug.LogError("Could not find PrototypeInfoData for DistrictType: " + districtType);
                return;
            }
            
            Vector3 position = GetAveragePosition();
            
            HashSet<Chunk> neighbours = new HashSet<Chunk>();
            List<Chunk> addedChunks = new List<Chunk>();
            foreach (Chunk chunk in chunks)
            {
                GetNeighbours(chunks, chunk, neighbours, 1);

                chunk.Clear(districtGenerator.ChunkWaveFunction.GameObjectPool);
                districtGenerator.ChunkWaveFunction.LoadCells(chunk, prototypeInfo);

                const int height = 1;
                for (int j = 0; j < height; j++) // Assumes that the chunks are not stacked vertically already
                {
                    int heightLevel = 1 + j;
                    Vector3 pos = chunk.Position + Vector3.up * districtGenerator.ChunkScale.y * heightLevel; 
                    addedChunks.Add(districtGenerator.ChunkWaveFunction.LoadChunk(pos, districtGenerator.ChunkSize, prototypeInfo));
                }
            }
            chunks.AddRange(addedChunks);
            
            foreach (Chunk chunk in neighbours)
            {
                chunk.Clear(districtGenerator.ChunkWaveFunction.GameObjectPool);
                districtGenerator.ChunkWaveFunction.LoadCells(chunk, chunk.PrototypeInfoData);
            }
            
            DistrictData districtData = GetDistrictData(districtType, chunks, position);
            uniqueDistricts.Add(districtData);
            
            foreach (Chunk chunk in chunks)
            {
                districts.TryAdd(chunk.ChunkIndex.xz, districtData);
            }

            OnDistrictCreated?.Invoke(districtData);
            districtGenerator.Run().Forget(Debug.LogError);
            return;


            Vector3 GetAveragePosition()
            {
                Vector3 vector3 = Vector3.zero;
                foreach (Chunk chunk in chunks)
                {
                    vector3 += chunk.Position;
                }
                vector3 /= chunks.Count;
                return vector3;
            }
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

        private DistrictData GetDistrictData(DistrictType districtType, HashSet<Chunk> chunks, Vector3 position)
        {
            DistrictData districtData = new DistrictData(districtType, chunks, position, districtGenerator, districtKey++);
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

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.Assertions;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using WaveFunctionCollapse;
using Sirenix.Utilities;
using Unity.Mathematics;
using Gameplay.Money;
using UnityEngine;
using Gameplay;
using System;
using System.Linq;

namespace Buildings.District
{
    public class DistrictHandler : SerializedMonoBehaviour
    {
        public event Action<DistrictData> OnDistrictCreated;
        
        [Title("District")]
        [SerializeField]
        private DistrictGenerator districtGenerator;
        
        [OdinSerialize]
        private Dictionary<DistrictType, PrototypeInfoData> districtInfoData = new Dictionary<DistrictType, PrototypeInfoData>();
        
        [Title("Debug")]
        [OdinSerialize, ReadOnly]
        private readonly Dictionary<int2, DistrictData> districts = new Dictionary<int2, DistrictData>();

        [OdinSerialize, ReadOnly]
        private readonly List<DistrictData> uniqueDistricts = new List<DistrictData>();

        private DistrictData townHallDistrict;

        private int districtKey;
        private bool inWave;
        
        public List<DistrictData> Districts => uniqueDistricts;

        private void OnEnable()
        {
            Events.OnWallsDestroyed += OnWallsDestroyed;
            Events.OnWaveStarted += OnWaveStarted;
            Events.OnWaveEnded += OnWaveEnded;
            
            districtGenerator.OnDistrictChunkRemoved += OnDistrictChunkRemoved;
        }

        private void OnDisable()
        {
            Events.OnWallsDestroyed += OnWallsDestroyed;
            Events.OnWaveStarted -= OnWaveStarted;
            Events.OnWaveEnded -= OnWaveEnded;
            
            districtGenerator.OnDistrictChunkRemoved -= OnDistrictChunkRemoved;
        }

        private void OnDistrictChunkRemoved(IChunk chunk)
        {
            foreach (DistrictData districtData in uniqueDistricts)
            {
                if (districtData.OnDistrictChunkRemoved(chunk))
                {
                    break;
                }
            }
        }

        private void OnWallsDestroyed(List<ChunkIndex> chunkIndexes)
        {
            districtGenerator.RemoveChunks(chunkIndexes).Forget();
        }

        private void OnWaveEnded()
        {
            inWave = false;
        }

        private void OnWaveStarted()
        {
            inWave = true;
        }

        private void Update()
        {
            if (!inWave || GameManager.Instance.IsGameOver) return;
            
            for (int i = 0; i < uniqueDistricts.Count; i++)
            {
                uniqueDistricts[i].Update();
            }
        }

        public void BuildDistrict(HashSet<QueryChunk> chunks, DistrictType districtType)
        {
            if (!districtInfoData.TryGetValue(districtType, out PrototypeInfoData prototypeInfo))
            {
                Debug.LogError("Could not find PrototypeInfoData for DistrictType: " + districtType);
                return;
            }
            
            HashSet<QueryChunk> neighbours = new HashSet<QueryChunk>();
            List<QueryChunk> addedChunks = new List<QueryChunk>();
            int topChunkCount = 0;
            foreach (QueryChunk chunk in chunks)
            {
                bool isTop = chunk.IsTop;
                if (isTop)
                {
                    topChunkCount++;
                }
                
                GetNeighbours(chunks, chunk, neighbours, 1);

                chunk.Clear(districtGenerator.ChunkWaveFunction.GameObjectPool);
                districtGenerator.ChunkWaveFunction.LoadCells(chunk, prototypeInfo);

                if (!isTop) continue;

                int height = districtType switch
                {
                    DistrictType.TownHall => (townHallDistrict?.State as TownHallState)?.UpgradeStats[0].Level - 1 ?? 0,
                    DistrictType.Archer => 1,
                    DistrictType.Bomb => 1,
                    DistrictType.Mine => 0,
                    _ => throw new ArgumentOutOfRangeException(nameof(districtType), districtType, null)
                };

                ChunkIndex? buildingChunkIndex = BuildingManager.Instance.GetIndex(chunk.Position + BuildingManager.Instance.CellSize / 2.0f);
                Assert.IsTrue(buildingChunkIndex.HasValue);
                for (int j = 0; j < height; j++) // Kinda assumes that the chunks are not stacked vertically already
                {
                    int heightLevel = 1 + j;
                    Vector3 pos = chunk.Position.XyZ(0) + Vector3.up * districtGenerator.ChunkScale.y * heightLevel;
                    int3 index = ChunkWaveUtility.GetDistrictIndex3(pos, districtGenerator.ChunkScale);
                    if (districtGenerator.ChunkWaveFunction.Chunks.TryGetValue(index, out QueryChunk existingChunk))
                    {
                        addedChunks.Add(existingChunk);
                        continue;
                    }

                    QueryChunk addChunk = districtGenerator.ChunkWaveFunction.LoadChunk(pos, districtGenerator.ChunkSize, prototypeInfo);
                    addedChunks.Add(addChunk);
                    
                    districtGenerator.ChunkIndexToChunks[buildingChunkIndex.Value].Add(addChunk.ChunkIndex);
                }
            }
            chunks.AddRange(addedChunks);
            
            foreach (QueryChunk chunk in neighbours)
            {
                chunk.Clear(districtGenerator.ChunkWaveFunction.GameObjectPool);
                districtGenerator.ChunkWaveFunction.LoadCells(chunk, chunk.PrototypeInfoData);
            }

            if (districtType == DistrictType.TownHall && CheckMerge(chunks, out DistrictData existingData))
            {
                foreach (QueryChunk chunk in chunks)
                {
                    districts.TryAdd(chunk.ChunkIndex.xz, existingData);
                }
                
                existingData.ExpandDistrict(chunks);
            }
            else
            {
                DistrictData districtData = GetDistrictData(districtType, chunks, topChunkCount);
            }
            
            districtGenerator.ChunkWaveFunction.Propagate();
            districtGenerator.Run().Forget(Debug.LogError);
            Events.OnDistrictBuilt?.Invoke(districtType);
        }

        private bool CheckMerge(HashSet<QueryChunk> chunks, out DistrictData districtData)
        {
            foreach (QueryChunk chunk in chunks)
            {
                if (districts.TryGetValue(chunk.ChunkIndex.xz, out districtData))
                {
                    return true;
                }
            }

            districtData = null;
            return false;
        }

        private static void GetNeighbours(HashSet<QueryChunk> chunks, IChunk chunk, HashSet<QueryChunk> neighbours, int depth)
        {
            foreach (IChunk adjacentChunk in chunk.AdjacentChunks)
            {
                if (adjacentChunk == null || chunks.Contains(adjacentChunk))
                {
                    continue;
                }
                    
                neighbours.Add(adjacentChunk as QueryChunk);
                if (depth > 0)
                {
                    GetNeighbours(chunks, adjacentChunk, neighbours, depth - 1);
                }
            }
        }

        private DistrictData GetDistrictData(DistrictType districtType, HashSet<QueryChunk> chunks, int topChunkCount)
        {
            Vector3 position = GetAveragePosition(chunks);
            DistrictData districtData = new DistrictData(districtType, chunks, position, districtGenerator, districtKey++)
            {
                GameSpeed = GameSpeedManager.Instance
            };
            
            uniqueDistricts.Add(districtData);
            if (districtType == DistrictType.TownHall) townHallDistrict = districtData;
                
            foreach (QueryChunk chunk in chunks)
            {
                districts.TryAdd(chunk.ChunkIndex.xz, districtData);
            }
            
            MoneyManager.Instance.Purchase(districtType, topChunkCount);
            OnDistrictCreated?.Invoke(districtData);

            districtData.OnDisposed += OnDispose;
            districtData.OnChunksLost += OnChunksLost;
            return districtData;
            
            
            void OnDispose()
            {
                districtData.OnDisposed -= OnDispose;
                districtData.OnChunksLost -= OnChunksLost;
                uniqueDistricts.Remove(districtData);

                foreach (QueryChunk chunk in chunks)
                {
                    districts.Remove(chunk.ChunkIndex.xz);
                }

                if (districtData.State is TownHallState)
                {
                    Events.OnCapitolDestroyed?.Invoke(districtData);
                }
            }

            void OnChunksLost(HashSet<int3> chunkIndexes)
            {
                List<int2> toRemove = new List<int2>(chunkIndexes.Count);
                foreach (KeyValuePair<int2, DistrictData> kvp in districts)
                {
                    if (kvp.Value != districtData) continue;
                    if (chunkIndexes.Any(chunkIndex => math.all(kvp.Key == chunkIndex.xz)))
                    {
                        toRemove.Add(kvp.Key);
                    }
                }

                for (int i = 0; i < toRemove.Count; i++)
                {
                    districts.Remove(toRemove[i]);
                }
            }
        }
        
        private Vector3 GetAveragePosition(IEnumerable<IChunk> chunks)
        {
            Vector3 vector3 = Vector3.zero;
            int count = 0;
            foreach (IChunk chunk in chunks)
            {
                count++;
                vector3 += chunk.Position;
            }
            vector3 /= count;
            return vector3;
        }

        public bool IsBuilt(IChunk chunk)
        {
            int2 index = chunk.ChunkIndex.xz;
            return districts.TryGetValue(index, out _);
        }
        
        public bool IsBuilt(IChunk chunk, out DistrictData districtData)
        {
            int2 index = chunk.ChunkIndex.xz;
            return districts.TryGetValue(index, out districtData);
        }
        
        public bool IsTownHallBuilt(IChunk chunk)
        {
            int2 index = chunk.ChunkIndex.xz;
            return districts.TryGetValue(index, out DistrictData districtData) && districtData.State is TownHallState;
        }
    }
}

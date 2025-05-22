using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.Assertions;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using WaveFunctionCollapse;
using Sirenix.Utilities;
using Unity.Mathematics;
using UnityEngine;
using System.Linq;
using Gameplay;
using System;
using Gameplay.Upgrades;

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

        private readonly Dictionary<DistrictType, int> districtAmounts = new Dictionary<DistrictType, int>();
        
        private DistrictData townHallDistrict;

        private int districtKey;
        private bool inWave;
        
        public List<DistrictData> Districts => uniqueDistricts;

        private void OnEnable()
        {
            Events.OnWallsDestroyed += OnWallsDestroyed;
            Events.OnDistrictBuilt += OnDistrictBuilt;
            Events.OnWaveStarted += OnWaveStarted;
            Events.OnWaveEnded += OnWaveEnded;
            
            districtGenerator.OnDistrictChunkRemoved += OnDistrictChunkRemoved;
        }

        private void OnDisable()
        {
            Events.OnWallsDestroyed -= OnWallsDestroyed;
            Events.OnDistrictBuilt -= OnDistrictBuilt;
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
            districtGenerator.AddAction(async () => await districtGenerator.RemoveChunks(chunkIndexes));
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

        public void AddBuiltDistrict(HashSet<QueryChunk> chunks, DistrictType districtType)
        {
            if (!districtInfoData.TryGetValue(districtType, out PrototypeInfoData prototypeInfo))
            {
                Debug.LogError("Could not find PrototypeInfoData for DistrictType: " + districtType);
                return;
            }
            
            if (CheckMerge(chunks, out DistrictData existingData))
            {
                foreach (QueryChunk chunk in chunks)
                {
                    districts.TryAdd(chunk.ChunkIndex.xz, existingData);
                }
                
                existingData.ExpandDistrict(chunks);
            }
            else
            {
                GetDistrictData(districtType, chunks);
            }

            foreach (QueryChunk chunk in chunks)
            {
                ChunkIndex? chunkIndex = districtGenerator.GetBuildingCell(chunk.ChunkIndex);
                Debug.Assert(chunkIndex != null, nameof(chunkIndex) + " != null");
                if (districtGenerator.ChunkIndexToChunks.TryGetValue(chunkIndex.Value, out HashSet<int3> list))
                {
                    list.Add(chunk.ChunkIndex);
                }
                else
                {
                    districtGenerator.ChunkIndexToChunks.Add(chunkIndex.Value, new HashSet<int3> { chunk.ChunkIndex });
                }
            }
            
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

        private DistrictData GetDistrictData(DistrictType districtType, HashSet<QueryChunk> chunks)
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

        public bool IsBuilt(int2 chunkIndex2)
        {
            return districts.ContainsKey(chunkIndex2);
        }
        
        public bool IsBuilt(IChunk chunk, out DistrictData districtData)
        {
            int2 index = chunk.ChunkIndex.xz;
            return districts.TryGetValue(index, out districtData);
        }
        
        private void OnDistrictBuilt(DistrictType districtType)
        {
            if (!districtAmounts.TryAdd(districtType, 1))
            {
                districtAmounts[districtType]++;
            }
        }
        
        public int GetDistrictAmount(DistrictType districtType)
        {
            return districtAmounts.GetValueOrDefault(districtType, 0);
        }
        
        public async UniTask IncreaseTownHallHeight()
        {
            HashSet<QueryChunk> addedChunks = new HashSet<QueryChunk>();

            foreach (DistrictData districtData in uniqueDistricts)
            {
                if ((districtData.State.CategoryType & CategoryType.TownHall) == 0) continue;
                
                foreach (QueryChunk queryChunk in districtData.DistrictChunks.Values)
                {
                    queryChunk.Clear(districtGenerator.ChunkWaveFunction.GameObjectPool);
                    districtGenerator.ClearChunkMeshes(queryChunk.ChunkIndex);
                    districtGenerator.ChunkWaveFunction.LoadCells(queryChunk, queryChunk.PrototypeInfoData);
                    addedChunks.Add(queryChunk);

                    if (!queryChunk.IsTop)
                    {
                        continue;
                    }

                    ChunkIndex? buildingChunkIndex = BuildingManager.Instance.GetIndex(queryChunk.Position);
                    Assert.IsTrue(buildingChunkIndex.HasValue);
                    
                    int heightLevel = queryChunk.ChunkIndex.y + 1;
                    Vector3 pos = queryChunk.Position.XyZ(0) + Vector3.up * (districtGenerator.ChunkScale.y * heightLevel);
                    int3 index = ChunkWaveUtility.GetDistrictIndex3(pos, districtGenerator.ChunkScale);
                    if (districtGenerator.ChunkWaveFunction.Chunks.TryGetValue(index, out QueryChunk existingChunk))
                    {
                        addedChunks.Add(existingChunk);
                        continue;
                    }
                    
                    QueryChunk addChunk = districtGenerator.ChunkWaveFunction.LoadChunk(pos, districtGenerator.ChunkSize, queryChunk.PrototypeInfoData);
                    addedChunks.Add(addChunk);

                    districtGenerator.ChunkIndexToChunks[buildingChunkIndex.Value].Add(addChunk.ChunkIndex);
                }
                    
                districtData.ExpandDistrict(addedChunks);
                break;
            }
            
            districtGenerator.ChunkWaveFunction.Propagate();
            await districtGenerator.Run(addedChunks);
        }
    }
}

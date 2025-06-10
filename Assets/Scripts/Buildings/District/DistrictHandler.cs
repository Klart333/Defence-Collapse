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
        
        [Title("Debug")]
        [OdinSerialize, ReadOnly]
        private readonly Dictionary<int2, DistrictData> districts = new Dictionary<int2, DistrictData>();

        [OdinSerialize, ReadOnly]
        private readonly HashSet<DistrictData> uniqueDistricts = new HashSet<DistrictData>();

        private readonly Dictionary<int2, HashSet<District>> districtObjects = new Dictionary<int2, HashSet<District>>();
        
        private readonly Dictionary<DistrictType, int> districtAmounts = new Dictionary<DistrictType, int>();
        
        private int districtKey;
        private bool inWave;
        
        public HashSet<DistrictData> Districts => uniqueDistricts;

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

            foreach (DistrictData districtData in uniqueDistricts)
            {
                districtData.Update();
            }
        }

        public void AddBuiltDistrict(HashSet<QueryChunk> chunks, DistrictType districtType)
        {
            if (CheckMerge(chunks, out HashSet<DistrictData> overlappingDistricts) > 0)
            {
                DistrictData districtToMergeInto = GetHighestLevel(overlappingDistricts);
                overlappingDistricts.Remove(districtToMergeInto);

                foreach (DistrictData data in overlappingDistricts)
                {
                    chunks.AddRange(data.DistrictChunks.Values);
                    data.Dispose();
                }
                
                foreach (QueryChunk chunk in chunks)
                {
                    districts.TryAdd(chunk.ChunkIndex.xz, districtToMergeInto);
                }
                
                districtToMergeInto.ExpandDistrict(chunks);
            }
            else
            {
                CreateDistrictData(districtType, chunks);
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

        private DistrictData GetHighestLevel(HashSet<DistrictData> datas)
        {
            int highest = -1;
            DistrictData highestDistrict = null;
            foreach (DistrictData data in datas)
            {
                if (data.State.Level <= highest) continue;
                
                highest = data.State.Level;
                highestDistrict = data;
            }
            
            return highestDistrict;
        }

        private int CheckMerge(HashSet<QueryChunk> chunks, out HashSet<DistrictData> overlappingDistricts)
        {
            overlappingDistricts = new HashSet<DistrictData>();
            foreach (QueryChunk chunk in chunks)
            {
                if (districts.TryGetValue(chunk.ChunkIndex.xz, out var districtData))
                {
                    overlappingDistricts.Add(districtData);
                }
            }

            return overlappingDistricts.Count;
        }

        private DistrictData CreateDistrictData(DistrictType districtType, HashSet<QueryChunk> chunks)
        {
            Vector3 position = GetAveragePosition(chunks);
            DistrictData districtData = new DistrictData(districtType, chunks, position, districtGenerator, districtKey++)
            {
                GameSpeed = GameSpeedManager.Instance,
                DistrictHandler = this
            };
            
            uniqueDistricts.Add(districtData);
                
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
                foreach (int3 chunkIndex in chunkIndexes)
                {
                    if (!districts.TryGetValue(chunkIndex.xz, out var data)) continue;
                    if (data != districtData) continue;
                    
                    districts.Remove(chunkIndex.xz);
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

        public void SetHoverOnObjects(ICollection<QueryChunk> chunks, bool isHover)
        {
            foreach (QueryChunk chunk in chunks)
            {
                if (!districtObjects.TryGetValue(chunk.ChunkIndex.xz, out HashSet<District> objects)) continue;
                
                foreach (District district in objects)
                {
                    district.Highlight(isHover);
                }
            }
        }

        public void AddDistrictObject(District district)
        {
            if (districtObjects.TryGetValue(district.ChunkIndex.Index.xz, out HashSet<District> list)) list.Add(district);
            else districtObjects.Add(district.ChunkIndex.Index.xz, new HashSet<District> { district });

        }

        public void RemoveDistrictObject(District district)
        {
            if (!districtObjects.TryGetValue(district.ChunkIndex.Index.xz, out HashSet<District> list)) return;

            list.Remove(district);
            if (list.Count <= 0) districtObjects.Remove(district.ChunkIndex.Index.xz);
        }
    }
}

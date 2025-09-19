using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Buildings.District.ECS;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using WaveFunctionCollapse;
using Sirenix.Utilities;
using Unity.Collections;
using Unity.Mathematics;
using Gameplay.Upgrades;
using Unity.Entities;
using Gameplay.Event;
using UnityEngine;
using Gameplay;
using System;

namespace Buildings.District
{
    public class DistrictHandler : SerializedMonoBehaviour
    {
        public event Action<DistrictData> OnDistrictDisplayed;
        public event Action<DistrictData> OnDistrictCreated;
        
        [Title("District")]
        [SerializeField]
        private DistrictGenerator districtGenerator;
        
        [SerializeField]
        private TowerDataUtility towerDataUtility;
        
        [SerializeField]
        private DistrictPrototypeInfoUtility prototypeInfoUtility;
        
        [Title("Debug")]
        [OdinSerialize, Sirenix.OdinInspector.ReadOnly]
        private readonly Dictionary<int2, DistrictData> districts = new Dictionary<int2, DistrictData>();

        public bool IsDebug;
        
        private readonly Dictionary<int2, HashSet<District>> districtObjects = new Dictionary<int2, HashSet<District>>();
        private readonly Dictionary<int, DistrictData> uniqueDistricts = new Dictionary<int, DistrictData>();
        private readonly Dictionary<DistrictType, int> districtAmounts = new Dictionary<DistrictType, int>();
        
        private EntityManager entityManager;
        private EntityQuery districtEntityQuery;
        
        private int districtKey;
        
        public Dictionary<int, DistrictData> Districts => uniqueDistricts;

        private void OnEnable()
        {
            Events.OnWallsDestroyed += OnWallsDestroyed;
            Events.OnDistrictBuilt += OnDistrictBuilt;
            Events.OnTurnIncreased += OnTurnIncreased;
            districtGenerator.OnDistrictChunkRemoved += OnDistrictChunkRemoved;
            Events.OnGameReset += OnGameReset;
            
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            districtEntityQuery = entityManager.CreateEntityQuery(typeof(DistrictEntityDataComponent));
        }

        private void OnDisable()
        {
            Events.OnWallsDestroyed -= OnWallsDestroyed;
            Events.OnDistrictBuilt -= OnDistrictBuilt;
            Events.OnTurnIncreased -= OnTurnIncreased;
            Events.OnGameReset -= OnGameReset;
            
            districtGenerator.OnDistrictChunkRemoved -= OnDistrictChunkRemoved;
        }

        private void OnGameReset()
        {
            districts.Clear();
        }

        private void Update()
        {
            foreach (DistrictData districtData in uniqueDistricts.Values)
            {
                districtData.Update();
            }
        }

        /// <summary>
        /// District Generator Rebuilding/Removing Chunk
        /// </summary>
        /// <param name="chunk"></param>
        private void OnDistrictChunkRemoved(IChunk chunk)
        {
            foreach (DistrictData districtData in uniqueDistricts.Values)
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

        
        private void OnTurnIncreased(int increase, int total)
        {
            UpdateDistrictEntities().Forget();
        }

        private async UniTaskVoid UpdateDistrictEntities()
        {
            await UniTask.NextFrame();
            
            NativeList<DistrictEntityDataComponent> array = districtEntityQuery.ToComponentDataListAsync<DistrictEntityDataComponent>(Allocator.TempJob, out var awaitJobHandle);
            awaitJobHandle.Complete();
            while (!awaitJobHandle.IsCompleted)
            {
                await UniTask.Yield();
            }

            if (!array.IsCreated) return;

            if (array.Length == 0)
            {
                array.Dispose();
                return;
            }
            
            foreach (DistrictEntityDataComponent data in array)
            {
                if (uniqueDistricts.TryGetValue(data.DistrictID, out DistrictData districtData))
                {
                    districtData.PerformAttack(data);
                }
                else
                {
                    Debug.Log("Data ID: " + data.DistrictID + " is attacking but it does not exist");
                }
            }
            
            array.Dispose();
            entityManager.DestroyEntity(districtEntityQuery);
            
            UpdateDistrictEntities().Forget();
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
                if (!districtGenerator.TryGetBuildingCell(chunk.ChunkIndex, out ChunkIndex chunkIndex))
                {
                    Debug.LogError("Should not happen");
                    return;
                }
                
                if (districtGenerator.ChunkIndexToChunks.TryGetValue(chunkIndex, out HashSet<int3> list)) list.Add(chunk.ChunkIndex);
                else districtGenerator.ChunkIndexToChunks.Add(chunkIndex, new HashSet<int3> { chunk.ChunkIndex });
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
            PrototypeInfoData prototypeInfo = prototypeInfoUtility.GetPrototypeInfo(districtType);
            TowerData towerData = towerDataUtility.GetTowerData(districtType);
            Vector3 position = GetAveragePosition(chunks);
            int key = districtKey++;
            DistrictData districtData = new DistrictData(towerData, chunks, position, districtGenerator, key, prototypeInfo)
            {
                GameSpeed = GameSpeedManager.Instance,
                DistrictHandler = this
            };
            
            uniqueDistricts.Add(key, districtData);
                
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
                uniqueDistricts.Remove(key);

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

            foreach (DistrictData districtData in uniqueDistricts.Values)
            {
                if ((districtData.State.CategoryType & CategoryType.TownHall) == 0) continue;
                
                foreach (QueryChunk queryChunk in districtData.DistrictChunks.Values)
                {
                    queryChunk.Clear(districtGenerator.ChunkWaveFunction.GameObjectPool);
                    districtGenerator.ClearChunkMeshes(queryChunk.ChunkIndex, false);
                    districtGenerator.ChunkWaveFunction.LoadCells(queryChunk, queryChunk.PrototypeInfoData);
                    addedChunks.Add(queryChunk);

                    if (!queryChunk.IsTop)
                    {
                        continue;
                    }

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

                    if (!districtGenerator.TryGetBuildingCell(queryChunk.ChunkIndex, out ChunkIndex buildingChunkIndex))
                    {
                        Debug.LogError("Should never get here");
                        return;
                    }
                    
                    districtGenerator.ChunkIndexToChunks[buildingChunkIndex].Add(addChunk.ChunkIndex);
                }
                    
                districtData.ExpandDistrict(addedChunks);
                break;
            }
            
            districtGenerator.ChunkWaveFunction.Propagate();
            await districtGenerator.Run(addedChunks, true);
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

        public void DisplayDistrictDisplay(DistrictData districtData)
        {
            OnDistrictDisplayed?.Invoke(districtData);
        }
    }
}

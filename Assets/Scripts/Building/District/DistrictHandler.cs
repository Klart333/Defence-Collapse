using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.Assertions;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using WaveFunctionCollapse;
using Sirenix.Utilities;
using Unity.Mathematics;
using UnityEngine;
using Gameplay;
using System;

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

        private void OnDistrictChunkRemoved(Chunk chunk)
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

        public void BuildDistrict(HashSet<Chunk> chunks, DistrictType districtType)
        {
            if (!districtInfoData.TryGetValue(districtType, out PrototypeInfoData prototypeInfo))
            {
                Debug.LogError("Could not find PrototypeInfoData for DistrictType: " + districtType);
                return;
            }
            
            HashSet<Chunk> neighbours = new HashSet<Chunk>();
            List<Chunk> addedChunks = new List<Chunk>();
            int topChunkCount = 0;
            foreach (Chunk chunk in chunks)
            {
                if (chunk.IsTop)
                {
                    topChunkCount++;
                }
                
                GetNeighbours(chunks, chunk, neighbours, 1);

                chunk.Clear(districtGenerator.ChunkWaveFunction.GameObjectPool);
                districtGenerator.ChunkWaveFunction.LoadCells(chunk, prototypeInfo);

                int height = districtType switch
                {
                    DistrictType.Archer => 1,
                    DistrictType.Bomb => 1,
                    DistrictType.Mine => 0,
                    //DistrictType.Church => expr,
                    _ => throw new ArgumentOutOfRangeException(nameof(districtType), districtType, null)
                };

                ChunkIndex? buildingChunkIndex = BuildingManager.Instance.GetIndex(chunk.Position + BuildingManager.Instance.GridScale / 2.0f);
                Assert.IsTrue(buildingChunkIndex.HasValue);
                for (int j = 0; j < height; j++) // Assumes that the chunks are not stacked vertically already
                {
                    int heightLevel = 1 + j;
                    Vector3 pos = chunk.Position + Vector3.up * districtGenerator.ChunkScale.y * heightLevel;
                    Chunk addChunk = districtGenerator.ChunkWaveFunction.LoadChunk(pos, districtGenerator.ChunkSize, prototypeInfo);
                    addedChunks.Add(addChunk);
                    
                    districtGenerator.ChunkIndexToChunks[buildingChunkIndex.Value].Add(addChunk.ChunkIndex);
                }
            }
            chunks.AddRange(addedChunks);
            
            foreach (Chunk chunk in neighbours)
            {
                chunk.Clear(districtGenerator.ChunkWaveFunction.GameObjectPool);
                districtGenerator.ChunkWaveFunction.LoadCells(chunk, chunk.PrototypeInfoData);
            }
            
            DistrictData districtData = GetDistrictData(districtType, chunks);
            uniqueDistricts.Add(districtData);
            
            foreach (Chunk chunk in chunks)
            {
                districts.TryAdd(chunk.ChunkIndex.xz, districtData);
            }
            
            districtData.OnDisposed += DistrictDataOnOnDisposed;
            
            MoneyManager.Instance.Purchase(districtType, topChunkCount);
            OnDistrictCreated?.Invoke(districtData);
            
            districtGenerator.ChunkWaveFunction.Propagate();
            districtGenerator.Run().Forget(Debug.LogError);
            
            void DistrictDataOnOnDisposed()
            {
                districtData.OnDisposed -= DistrictDataOnOnDisposed;
                uniqueDistricts.Remove(districtData);
            
                foreach (Chunk chunk in chunks)
                {
                    districts.Remove(chunk.ChunkIndex.xz);
                }

                if (districtData.State is MineState { IsCapitol: true })
                {
                    Events.OnCapitolDestroyed?.Invoke(districtData);
                }
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

        private DistrictData GetDistrictData(DistrictType districtType, HashSet<Chunk> chunks)
        {
            Vector3 position = GetAveragePosition(chunks);
            DistrictData districtData = new DistrictData(districtType, chunks, position, districtGenerator, districtKey++)
            {
                GameSpeed = GameSpeedManager.Instance
            };
            return districtData;
        }
        
        private Vector3 GetAveragePosition(IEnumerable<Chunk> chunks)
        {
            Vector3 vector3 = Vector3.zero;
            int count = 0;
            foreach (Chunk chunk in chunks)
            {
                count++;
                vector3 += chunk.Position;
            }
            vector3 /= count;
            return vector3;
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
                DistrictType.Bomb => width >= 3 && depth >= 3,
                DistrictType.Church => width >= 2 && depth >= 2,
                _ => true
            };
        }
    }
}

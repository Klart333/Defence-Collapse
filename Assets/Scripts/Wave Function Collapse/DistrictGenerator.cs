using System.Collections.Generic;
using Debug = UnityEngine.Debug;
using Cysharp.Threading.Tasks;
using Sirenix.Serialization;
using Sirenix.OdinInspector;
using System.Diagnostics;
using Buildings.District;
using Sirenix.Utilities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;

namespace WaveFunctionCollapse
{
    public class DistrictGenerator : SerializedMonoBehaviour, IChunkWaveFunction<QueryChunk>
    {
        public event Action<QueryChunk> OnDistrictChunkRemoved;
        
        [Title("Wave Function")]
        [SerializeField]
        private ChunkWaveFunction<QueryChunk> waveFunction;

        [SerializeField]
        private Vector3Int chunkSize;
        
        [SerializeField]
        private PrototypeInfoData defaultPrototypeInfoData;

        [SerializeField]
        private District districtPrefab;
        
        [Title("Data")]
        [SerializeField]
        private BuildableCornerData buildableCornerData;

        [SerializeField]
        private DistrictHandler districtHandler;
        
        [Title("Settings")]
        [SerializeField]
        private int awaitEveryFrame = 1;

        [SerializeField, ShowIf(nameof(ShouldAwait))]
        private int awaitTimeMs = 1;

        [SerializeField]
        private float maxMillisecondsPerFrame = 4;

        [Title("Debug")]
        [SerializeField]
        private bool debug;

        [OdinSerialize, ReadOnly]
        public readonly Dictionary<ChunkIndex, List<int3>> ChunkIndexToChunks = new Dictionary<ChunkIndex, List<int3>>();
        
        public Dictionary<ChunkIndex, IBuildable> QuerySpawnedBuildings { get; }
        public Dictionary<ChunkIndex, IBuildable> SpawnedMeshes { get; }
        
        private readonly Queue<List<IBuildable>> buildQueue = new Queue<List<IBuildable>>();
        private List<QueryChunk> queriedChunks = new List<QueryChunk>();

        private BuildingAnimator buildingAnimator;
        private Vector3 offset;

        private bool isUpdatingChunks;

        public Vector3 ChunkScale => new Vector3(chunkSize.x * ChunkWaveFunction.CellSize.x, chunkSize.y * ChunkWaveFunction.CellSize.y, chunkSize.z * ChunkWaveFunction.CellSize.z);
        public Vector3 CellSize => waveFunction.CellSize;
        public Vector3Int ChunkSize => chunkSize;
        
        public ChunkWaveFunction<QueryChunk> ChunkWaveFunction => waveFunction;
        private bool ShouldAwait => awaitEveryFrame > 0;
        public bool IsGenerating {get; private set;}

        private void OnEnable()
        {
            offset = new Vector3(waveFunction.CellSize.x, 0, waveFunction.CellSize.z) / -2.0f;
            buildingAnimator = FindAnyObjectByType<BuildingAnimator>();

            waveFunction.Load(this);
            //Events.OnBuildingBuilt += OnBuildingBuilt;
        }

        private void OnDisable()
        {
            //Events.OnBuildingBuilt -= OnBuildingBuilt;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                foreach (QueryChunk chunk in waveFunction.Chunks.Values)
                {
                    chunk.Clear(waveFunction.GameObjectPool);
                }
                
                foreach (QueryChunk chunk in waveFunction.Chunks.Values)
                {
                    waveFunction.LoadCells(chunk, chunk.PrototypeInfoData);
                }

                Run().Forget(Debug.LogError);
            }
        }

        private void OnBuildingBuilt(IEnumerable<IBuildable> buildables)
        {
            buildQueue.Enqueue(new List<IBuildable>(buildables));

            if (!isUpdatingChunks)
            {
                UpdateChunks().Forget();
            }
        }

        private async UniTaskVoid UpdateChunks()
        {
            isUpdatingChunks = true;

            while (buildQueue.TryDequeue(out List<IBuildable> buildables))
            {
                List<Vector3> positions = new List<Vector3>();
                HashSet<QueryChunk> overrideChunks = new HashSet<QueryChunk>();
                foreach (IBuildable buildable in buildables)
                {
                    HandleBuildable(buildable, overrideChunks, positions);
                }

                foreach (QueryChunk chunk in overrideChunks)
                {
                    if (chunk.IsRemoved)
                    {
                        continue;
                    }

                    waveFunction.LoadCells(chunk, chunk.PrototypeInfoData);
                }

                foreach (Vector3 pos in positions)
                {
                    overrideChunks.Add(waveFunction.LoadChunk(pos, chunkSize, defaultPrototypeInfoData));
                }

                waveFunction.Propagate();
                await Run();
                await UniTask.Yield();

                await IterativeFailChecks(overrideChunks);
            }

            isUpdatingChunks = false;

            void HandleBuildable(IBuildable buildable, HashSet<QueryChunk> overrideChunks, List<Vector3> positions)
            {
                for (int i = 0; i < WaveFunctionUtility.Corners.Length; i++)
                {
                    bool isBuildable = buildableCornerData.IsCornerBuildable(buildable.MeshRot, WaveFunctionUtility.Corners[i].ToVector2Int(), out bool meshIsBuildable);
                    isBuildable |= buildable.MeshRot.MeshIndex == -1;
                    if (!isBuildable && !meshIsBuildable) continue;

                    Vector3 cornerOffset = new Vector3(WaveFunctionUtility.Corners[i].x * chunkSize.x * waveFunction.CellSize.x, 0, WaveFunctionUtility.Corners[i].y * chunkSize.z * waveFunction.CellSize.z) * 0.5f;
                    Vector3 pos = buildable.gameObject.transform.position + cornerOffset + offset;
                    int3 index = ChunkWaveUtility.GetDistrictIndex3(pos, ChunkScale);
                    if (waveFunction.Chunks.TryGetValue(index, out QueryChunk chunk))
                    {
                        Queue<QueryChunk> chunkQueue = new Queue<QueryChunk>();
                        chunkQueue.Enqueue(chunk);

                        while (chunkQueue.TryDequeue(out chunk))
                        {
                            bool isDistrict = districtHandler.IsBuilt(chunk);
                            if (isDistrict)
                            {
                                int3 aboveIndex = chunk.ChunkIndex + new int3(0, 1, 0);
                                if (waveFunction.Chunks.TryGetValue(aboveIndex, out QueryChunk aboveChunk))
                                {
                                    chunkQueue.Enqueue(aboveChunk);
                                }
                            }
                            
                            if (isBuildable)
                            {
                                chunk.Clear(waveFunction.GameObjectPool);
                                overrideChunks.Add(chunk);
                            }
                            else
                            {
                                OnDistrictChunkRemoved?.Invoke(chunk);
                                waveFunction.RemoveChunk(chunk.ChunkIndex, out List<QueryChunk> neighbourChunks);
                                for (int j = 0; j < neighbourChunks.Count; j++)
                                {
                                    ResetNeighbours(overrideChunks, neighbourChunks[j], 1);
                                }
                            }   
                        }
                    }
                    else if (isBuildable)
                    {
                        positions.Add(pos);
                        if (ChunkIndexToChunks.TryGetValue(buildable.ChunkIndex, out List<int3> list))
                        {
                            list.Add(index);
                        }
                        else
                        {
                            ChunkIndexToChunks.Add(buildable.ChunkIndex, new List<int3> { index });
                        }
                    }
                }
            }
        }

        private async UniTask IterativeFailChecks(HashSet<QueryChunk> chunksToCollapse)
        {
            while (CheckFailed(chunksToCollapse))
            {
                HashSet<QueryChunk> neighbours = new HashSet<QueryChunk>();
                foreach (QueryChunk overrideChunk in chunksToCollapse)
                {
                    for (int i = 0; i < overrideChunk.AdjacentChunks.Length; i++)
                    {
                        if (overrideChunk.AdjacentChunks[i] is not QueryChunk || chunksToCollapse.Contains(overrideChunk)) continue;

                        neighbours.Add(overrideChunk);
                    }
                }

                chunksToCollapse.AddRange(neighbours);

                foreach (QueryChunk chunk in chunksToCollapse)
                {
                    chunk.Clear(waveFunction.GameObjectPool);
                    waveFunction.LoadCells(chunk, defaultPrototypeInfoData);
                }

                await Run();
                await UniTask.Yield();
            }
        }

        private bool CheckFailed(IEnumerable<IChunk> overrideChunks)
        {
            int minValid = 2;
            int count = 0;
            foreach (IChunk chunk in overrideChunks)
            {
                count++;
                foreach (Cell cell in chunk.Cells)
                {
                    if (cell.PossiblePrototypes[0].MeshRot.MeshIndex != -1)
                    {
                        minValid--;
                        break;
                    }
                }

                if (minValid <= 0)
                {
                    break;
                }
            }

            return count > 2 && minValid > 0;
        }

        private void ResetNeighbours(HashSet<QueryChunk> overrideChunks, QueryChunk neighbourChunk, int depth)
        {
            for (int i = 0; i < neighbourChunk.AdjacentChunks.Length; i++)
            {
                if (neighbourChunk.AdjacentChunks[i] == null) continue;

                if (overrideChunks.Add(neighbourChunk.AdjacentChunks[i] as QueryChunk))
                {
                    neighbourChunk.AdjacentChunks[i].Clear(waveFunction.GameObjectPool);
                    if (depth > 0)
                    {
                        ResetNeighbours(overrideChunks, neighbourChunk.AdjacentChunks[i] as QueryChunk, depth - 1);
                    }
                }
            }
        }

        public async UniTaskVoid RemoveChunks(List<ChunkIndex> chunkIndexes)
        {
            await UniTask.WaitWhile(() => IsGenerating);

            HashSet<QueryChunk> neighbours = new HashSet<QueryChunk>();
            HashSet<int3> killIndexes = new HashSet<int3>();
            for (int i = 0; i < chunkIndexes.Count; i++)
            {
                if (!ChunkIndexToChunks.TryGetValue(chunkIndexes[i], out List<int3> indexes))
                {
                    continue;
                }
                    
                for (int j = indexes.Count - 1; j >= 0; j--)
                {
                    if (!waveFunction.Chunks.ContainsKey(indexes[j]))
                    {
                        indexes.RemoveAt(j);
                        continue;
                    }
                    
                    killIndexes.Add(indexes[j]);
                    ResetNeighbours(neighbours, waveFunction.Chunks[indexes[j]], 1);
                }
                
                ChunkIndexToChunks.Remove(chunkIndexes[i]);
            }

            foreach (int3 index in killIndexes)
            {
                neighbours.Remove(waveFunction.Chunks[index]);
                waveFunction.RemoveChunk(index);
            }
            
            foreach (IChunk neighbour in neighbours)
            {
                waveFunction.LoadCells(neighbour as QueryChunk, neighbour.PrototypeInfoData);
            }
            
            waveFunction.Propagate();
            
            await Run();
            await UniTask.Yield();

            IterativeFailChecks(neighbours).Forget(Debug.LogError);
        }

        public async UniTask Run()
        {
            await UniTask.WaitWhile(() => IsGenerating);

            IsGenerating = true;
            Stopwatch watch = Stopwatch.StartNew();
            int frameCount = 0;
            while (!waveFunction.AllCollapsed())
            {
                watch.Start();
                waveFunction.Iterate();
                watch.Stop();

                if (awaitEveryFrame > 0 && ++frameCount % awaitEveryFrame == 0)
                {
                    frameCount = 0;
                    await UniTask.Delay(awaitTimeMs);
                }

                if (watch.ElapsedMilliseconds < maxMillisecondsPerFrame) continue;

                await UniTask.NextFrame();

                watch.Restart();
            }

            IsGenerating = false;
        }

        public IBuildable GenerateMesh(Vector3 position, PrototypeData prototypeData, bool animate = false)
        {
            District building = districtPrefab.GetAtPosAndRot<District>(position, Quaternion.Euler(0, 90 * prototypeData.MeshRot.Rot, 0)); 

            building.Setup(prototypeData, waveFunction.CellSize);

            if (animate) buildingAnimator.Animate(building);

            return building;
        }
        
        #region Query & Place

        public void Place()
        {
            Events.OnBuildingBuilt?.Invoke(QuerySpawnedBuildings.Values);
            
            foreach (QueryChunk chunk in queriedChunks)
            {
                chunk.Place(ChunkWaveFunction.SetCell);
            }
            
            foreach (KeyValuePair<ChunkIndex, IBuildable> item in QuerySpawnedBuildings)
            {
                SpawnedMeshes.Add(item.Key, item.Value);
            }

            foreach (IBuildable item in QuerySpawnedBuildings.Values)
            {
                item.ToggleIsBuildableVisual(false, false);
            }
            
            QuerySpawnedBuildings.Clear();
        }

        public void RevertQuery()
        {
            if (QuerySpawnedBuildings.Count == 0)
            {
                return;
            }
            
            foreach (QueryChunk chunk in queriedChunks)
            {
                chunk.RevertQuery(ChunkWaveFunction.SetCell);
            }
            
            foreach (IBuildable item in QuerySpawnedBuildings.Values)
            {
                item.gameObject.SetActive(false);
            }
            QuerySpawnedBuildings.Clear();
        }
        
        public Dictionary<ChunkIndex, IBuildable> Query(int2[,] cellsToCollapse, PrototypeInfoData prototypeInfoData)
        {
            RevertQuery();

            if (cellsToCollapse.Length <= 0) return QuerySpawnedBuildings;

            // Plan: Add chunks at indicies if not already there
            // If already there -> Query clear the chunk
            // Also query clear adjacent chunks, so that they can merge 
            // (optional) Give access to collapse some of the top cells into the shooting ones, could add that info to the district script.  

            waveFunction.Propagate();

            IsGenerating = true;
            int tries = 1000;
            while (!IsGenerating)//cellsToCollapse.Any(x => !waveFunction[x].Collapsed) && tries-- > 0)
            {
                ChunkIndex index = waveFunction.GetLowestEntropyIndex();
                PrototypeData chosenPrototype = waveFunction.Collapse(waveFunction[index]);
                ChunkWaveFunction.SetCell(index, chosenPrototype);

                waveFunction.Propagate();
            }
            IsGenerating = false;

            if (tries <= 0)
            {
                Debug.LogError("Ran out of attempts to collapse");
            }

            return QuerySpawnedBuildings;
        }

        #endregion
        
        #region Debug

#if UNITY_EDITOR
        public void OnDrawGizmosSelected()
        {
            if (!EditorApplication.isPlaying || !debug)
            {
                return;
            }

            foreach (IChunk chunk in waveFunction.Chunks.Values)
            {
                foreach (Cell cell in chunk.Cells)
                {
                    Vector3 pos = cell.Position;
                    Gizmos.color = cell.Buildable ? Color.white : Color.red;

                    Gizmos.DrawWireCube(pos, new Vector3(waveFunction.CellSize.x, waveFunction.CellSize.y, waveFunction.CellSize.z) * 0.75f);
                }
            }
        }
#endif
        #endregion
    }
}
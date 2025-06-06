using System.Collections.Generic;
using Debug = UnityEngine.Debug;
using Cysharp.Threading.Tasks;
using Sirenix.Serialization;
using Sirenix.OdinInspector;
using System.Diagnostics;
using Buildings.District;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using System.Linq;
using Gameplay;
using System;
using Buildings;

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
        private bool verbose;

        [OdinSerialize]
        public readonly Dictionary<ChunkIndex, HashSet<int3>> ChunkIndexToChunks = new Dictionary<ChunkIndex, HashSet<int3>>();

        public Dictionary<int3, PrototypeInfoData> QueryChangedData { get; } = new Dictionary<int3, PrototypeInfoData>();
        public Dictionary<ChunkIndex, IBuildable> QuerySpawnedBuildings { get; } = new Dictionary<ChunkIndex, IBuildable>();
        public Dictionary<ChunkIndex, IBuildable> SpawnedMeshes { get; } = new Dictionary<ChunkIndex, IBuildable>();
        
        private readonly Queue<List<IBuildable>> buildQueue = new Queue<List<IBuildable>>();
        private HashSet<QueryChunk> queriedChunks = new HashSet<QueryChunk>();
        
        private readonly Queue<Func<UniTask>> GeneratorActionQueue = new Queue<Func<UniTask>>();

        private BuildingAnimator buildingAnimator;
        private Vector3 offset;

        private bool isUpdatingChunks;
        private bool isRemovingChunks;

        public Vector3 ChunkScale => new Vector3(chunkSize.x * ChunkWaveFunction.CellSize.x, chunkSize.y * ChunkWaveFunction.CellSize.y, chunkSize.z * ChunkWaveFunction.CellSize.z);
        public HashSet<QueryChunk> QueriedChunks => queriedChunks;
        public Vector3 CellSize => waveFunction.CellSize;
        public Vector3Int ChunkSize => chunkSize;
        
        public ChunkWaveFunction<QueryChunk> ChunkWaveFunction => waveFunction;
        private bool ShouldAwait => awaitEveryFrame > 0;
        public bool IsGenerating { get; private set; }

        private void OnEnable()
        {
            offset = new Vector3(waveFunction.CellSize.x, 0, waveFunction.CellSize.z) / 2.0f;
            buildingAnimator = FindAnyObjectByType<BuildingAnimator>();

            waveFunction.Load(this);
            Events.OnBuildingBuilt += OnBuildingBuilt;

            UpdateActions().Forget();
        }
        
        private void OnDisable()
        {
            Events.OnBuildingBuilt -= OnBuildingBuilt;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                IsGenerating = false;
                foreach (QueryChunk chunk in waveFunction.Chunks.Values)
                {
                    chunk.Clear(waveFunction.GameObjectPool);
                    ClearChunkMeshes(chunk.ChunkIndex);
                }
                
                foreach (QueryChunk chunk in waveFunction.Chunks.Values)
                {
                    waveFunction.LoadCells(chunk, chunk.PrototypeInfoData);
                }

                Run(waveFunction.Chunks.Values).Forget();
            }
        }
        
        private async UniTaskVoid UpdateActions()
        {
            GameManager gameManager = await GameManager.Get();
            while (!gameManager.IsGameOver)
            {
                if (GeneratorActionQueue.TryDequeue(out Func<UniTask> action))
                {
                    await action.Invoke();
                }
                
                await UniTask.Yield();
            }    
        }

        public void AddAction(Func<UniTask> action)
        {
            GeneratorActionQueue.Enqueue(action);
        }

        private void OnBuildingBuilt(ICollection<IBuildable> buildables)
        {
            if (buildables?.FirstOrDefault() is not Building) return;
            
            buildQueue.Enqueue(new List<IBuildable>(buildables));

            AddAction(UpdateChunksIfNotAlready);
        }

        private async UniTask UpdateChunksIfNotAlready()
        {
            await UniTask.DelayFrame(2);

            if (isUpdatingChunks)
            {
                return;
            }
            
            isUpdatingChunks = true;
            await UpdateChunks();
        }

        private async UniTask UpdateChunks()
        {
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
                await Run(overrideChunks.Where(x => !x.IsRemoved).ToList());
            }

            isUpdatingChunks = false;

            void HandleBuildable(IBuildable buildable, HashSet<QueryChunk> overrideChunks, List<Vector3> positions)
            {
                for (int i = 0; i < WaveFunctionUtility.Corners.Length; i++)
                {
                    bool isBuildable = buildableCornerData.IsCornerBuildable(buildable.MeshRot, -WaveFunctionUtility.Corners[i], out bool meshIsBuildable);
                    if (!isBuildable && !meshIsBuildable) continue; 

                    Vector3 cornerOffset = new Vector3(WaveFunctionUtility.Corners[i].x * chunkSize.x * waveFunction.CellSize.x, 0, WaveFunctionUtility.Corners[i].y * chunkSize.z * waveFunction.CellSize.z) * 0.5f;
                    Vector3 pos = buildable.gameObject.transform.position - cornerOffset - offset;
                    int3 index = ChunkWaveUtility.GetDistrictIndex3(pos, ChunkScale);
                    if (waveFunction.Chunks.TryGetValue(index, out QueryChunk chunk))
                    {
                        HandleBuiltChunk(chunk, isBuildable, overrideChunks, buildable.ChunkIndex);
                    }
                    else if (isBuildable)
                    {
                        positions.Add(pos);
                        if (ChunkIndexToChunks.TryGetValue(buildable.ChunkIndex, out HashSet<int3> list))
                        {
                            list.Add(index);
                        }
                        else
                        {
                            ChunkIndexToChunks.Add(buildable.ChunkIndex, new HashSet<int3> { index });
                        }
                    }
                }
            }

            void HandleBuiltChunk(QueryChunk chunk, bool isBuildable, HashSet<QueryChunk> overrideChunks, ChunkIndex buildIndex)
            {
                Queue<QueryChunk> chunkQueue = new Queue<QueryChunk>();
                chunkQueue.Enqueue(chunk);

                while (chunkQueue.TryDequeue(out chunk))
                {
                    bool isDistrict = chunk.PrototypeInfoData != defaultPrototypeInfoData;
                    
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
                        ClearChunkMeshes(chunk.ChunkIndex);
                        overrideChunks.Add(chunk);
                    }
                    else
                    {
                        OnDistrictChunkRemoved?.Invoke(chunk);
                        waveFunction.RemoveChunk(chunk.ChunkIndex, out List<QueryChunk> neighbourChunks);
                        ClearChunkMeshes(chunk.ChunkIndex);
                        for (int i = 0; i < neighbourChunks.Count; i++)
                        {
                            ResetNeighbours(overrideChunks, neighbourChunks[i], 1);
                        }

                        if (!ChunkIndexToChunks.TryGetValue(buildIndex, out var list))
                        {
                            Debug.LogError("Could not find DistrictChunk at buildIndex: " + buildIndex);
                            continue;
                        }
                        list.Remove(chunk.ChunkIndex);
                        if (list.Count == 0)
                        {
                            ChunkIndexToChunks.Remove(buildIndex);
                        }
                    } 
                }
            }
        }

        public void ResetNeighbours(HashSet<QueryChunk> overrideChunks, QueryChunk neighbourChunk, int depth)
        {
            for (int i = 0; i < neighbourChunk.AdjacentChunks.Length; i++)
            {
                if (neighbourChunk.AdjacentChunks[i] == null) continue;

                if (!overrideChunks.Add(neighbourChunk.AdjacentChunks[i] as QueryChunk) 
                    || neighbourChunk.AdjacentChunks[i] is not QueryChunk adjacentChunk) continue;

                adjacentChunk.Clear(ChunkWaveFunction.GameObjectPool);
                ClearChunkMeshes(adjacentChunk.ChunkIndex);
                
                if (depth > 0)
                {
                    ResetNeighbours(overrideChunks, neighbourChunk.AdjacentChunks[i] as QueryChunk, depth - 1);
                }
            }
        }

        public async UniTask RemoveChunks(List<ChunkIndex> chunkIndexes)
        {
            isRemovingChunks = true;

            HashSet<QueryChunk> neighbours = new HashSet<QueryChunk>();
            HashSet<int3> killIndexes = new HashSet<int3>();
            for (int i = 0; i < chunkIndexes.Count; i++)
            {
                if (!ChunkIndexToChunks.TryGetValue(chunkIndexes[i], out HashSet<int3> indexes))
                {
                    continue;
                }

                foreach (int3 index in indexes)
                {
                    if (!waveFunction.Chunks.ContainsKey(index))
                    {
                        continue;
                    }
                    
                    killIndexes.Add(index);
                    ResetNeighbours(neighbours, waveFunction.Chunks[index], 1);
                }
                
                ChunkIndexToChunks.Remove(chunkIndexes[i]);
            }

            foreach (int3 index in killIndexes)
            {
                neighbours.Remove(waveFunction.Chunks[index]);
                waveFunction.RemoveChunk(index);
                ClearChunkMeshes(index);
            }
            
            foreach (QueryChunk neighbour in neighbours)
            {
                if (!districtHandler.IsBuilt(neighbour.ChunkIndex.xz))
                {
                    waveFunction.LoadCells(neighbour, neighbour.PrototypeInfoData);
                }
            }
            
            waveFunction.Propagate();

            if (neighbours.Count <= 0)
            {
                isRemovingChunks = false;
                return;
            }

            await Run(neighbours);
            isRemovingChunks = false;
        }

        public async UniTask Run(ICollection<QueryChunk> chunksToCollapse)
        {
            if (IsGenerating)
            {
                await UniTask.WaitWhile(() => IsGenerating);
            }
            
            IsGenerating = true;
            Stopwatch watch = Stopwatch.StartNew();
            int frameCount = 0;
            while (chunksToCollapse.Any(x => !x.AllCollapsed))
            {
                watch.Start();
                ChunkIndex? index = waveFunction.GetLowestEntropyIndex(chunksToCollapse);
                if (!index.HasValue)
                {
                    //Debug.LogError("Could not find lowest entropy index");
                    IsGenerating = false;
                    return;
                }
                PrototypeData chosenPrototype = waveFunction.Collapse(waveFunction[index.Value]);
                SetCell(index.Value, chosenPrototype, false);

                waveFunction.Propagate();
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

            foreach (QueryChunk chunk in chunksToCollapse)
            {
                chunk.Place();
            }
            
            queriedChunks.Clear();
            Place();

            IsGenerating = false;
        }
        
        #region Query & Place

        public void Place()
        {
            Events.OnBuildingBuilt?.Invoke(QuerySpawnedBuildings.Values);
            foreach (QueryChunk chunk in queriedChunks)
            {
                chunk.Place();
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
            QueryChangedData.Clear();
            queriedChunks.Clear();
        }

        public void RevertQuery()
        {
            if (QuerySpawnedBuildings.Count == 0)
            {
                return;
            }
            
            foreach (KeyValuePair<int3,PrototypeInfoData> keyValuePair in QueryChangedData)
            {
                ChunkWaveFunction.Chunks[keyValuePair.Key].PrototypeInfoData = keyValuePair.Value;
            }
            
            foreach (QueryChunk chunk in queriedChunks)
            {
                chunk.RevertQuery(SetCell, ChunkWaveFunction.RemoveChunk);
            }
            
            foreach (IBuildable item in QuerySpawnedBuildings.Values)
            {
                item.gameObject.SetActive(false);
            }
            
            QueryChangedData.Clear();
            QuerySpawnedBuildings.Clear();
            queriedChunks.Clear();
        }
        
        public Dictionary<ChunkIndex, IBuildable> Query(int2[,] cellsToCollapse, int height, PrototypeInfoData prototypeInfoData)
        {
            if (IsGenerating || isRemovingChunks || isUpdatingChunks)
            {
                return null;
            }
            
            RevertQuery();

            if (cellsToCollapse.Length <= 0) return QuerySpawnedBuildings;
            
            for (int x = 0; x < cellsToCollapse.GetLength(0); x++)
            for (int z = 0; z < cellsToCollapse.GetLength(1); z++)
            for (int y = 0; y < height; y++)
            {
                int3 index = cellsToCollapse[x, z].XyZ(y);
                if (ChunkWaveFunction.Chunks.TryGetValue(index, out QueryChunk chunkAtIndex))
                {
                    // It's not a district because then it's not valid to query (from the DistrictPlacer)
                    queriedChunks.Add(chunkAtIndex);
                    QueryChangedData.Add(index, chunkAtIndex.PrototypeInfoData);
                    chunkAtIndex.QueryLoad(prototypeInfoData, CellSize, ChunkWaveFunction.CellStack);

                    ClearChunkMeshes(chunkAtIndex.ChunkIndex);   
                }
                else
                {
                    chunkAtIndex = ChunkWaveFunction.LoadChunk(index, ChunkSize, prototypeInfoData);
                    queriedChunks.Add(chunkAtIndex);
                }
                
                QueryResetNeighbours(queriedChunks, chunkAtIndex, 2);
            }

            waveFunction.Propagate();

            IsGenerating = true;
            int tries = 1000;
            while (queriedChunks.Any(x => !x.IsClear && !x.AllCollapsed) && tries-- > 0)
            {
                ChunkIndex? index = waveFunction.GetLowestEntropyIndex(queriedChunks);
                if (!index.HasValue)
                {
                    Debug.LogError("Could not find lowest entropy index");
                    return null;
                }
                PrototypeData chosenPrototype = waveFunction.Collapse(waveFunction[index.Value]);
                SetCell(index.Value, chosenPrototype);

                waveFunction.Propagate();
            }
            IsGenerating = false;

            if (tries <= 0)
            {
                Debug.LogError("Ran out of attempts to collapse");
            }

            return QuerySpawnedBuildings;
        }
        
        private void QueryResetNeighbours(HashSet<QueryChunk> overrideChunks, QueryChunk neighbourChunk, int depth)
        {
            for (int i = 0; i < neighbourChunk.AdjacentChunks.Length; i++)
            {
                if (neighbourChunk.AdjacentChunks[i] == null 
                    || neighbourChunk.AdjacentChunks[i] is not QueryChunk adjacentChunk) continue;

                if (!overrideChunks.Add(adjacentChunk)) continue;

                adjacentChunk.QueryLoad(adjacentChunk.PrototypeInfoData, ChunkWaveFunction.CellSize, ChunkWaveFunction.CellStack);
                ClearChunkMeshes(adjacentChunk.ChunkIndex);
                
                if (depth > 0)
                {
                    QueryResetNeighbours(overrideChunks, adjacentChunk, depth - 1);
                }
            }
        }

        public void ClearChunkMeshes(int3 chunkIndex)
        {
            for (int x = 0; x < chunkSize.x; x++)
            for (int y = 0; y < chunkSize.y; y++)
            for (int z = 0; z < chunkSize.z; z++)
            {
                ChunkIndex index = new ChunkIndex(chunkIndex, new int3(x, y, z));
                if (SpawnedMeshes.TryGetValue(index, out IBuildable buildable))
                {
                    buildable.gameObject.SetActive(false);
                    SpawnedMeshes.Remove(index);
                }
            }
        }

        public void SetCell(ChunkIndex index, PrototypeData chosenPrototype, bool query = true)
        {
            Cell cell = new Cell(true, ChunkWaveFunction[index].Position, new List<PrototypeData> { chosenPrototype });
            ChunkWaveFunction[index] = cell;
            ChunkWaveFunction.CellStack.Push(index);
            if (chosenPrototype.MeshRot.MeshIndex == -1)
            {
                return;
            }

            IBuildable spawned = GenerateMesh(ChunkWaveFunction[index].Position, index, chosenPrototype);

            if (query) 
            {
                QuerySpawnedBuildings.Add(index, spawned);
                return;
            }

            spawned.ToggleIsBuildableVisual(false, false);
            SpawnedMeshes[index] = spawned;
        }
        
        public IBuildable GenerateMesh(Vector3 position, ChunkIndex index, PrototypeData prototypeData, bool animate = false)
        {
            District building = districtPrefab.GetAtPosAndRot<District>(position + offset, Quaternion.Euler(0, 90 * prototypeData.MeshRot.Rot, 0)); 

            building.Setup(prototypeData, index, waveFunction.CellSize / 2.0f);

            if (animate) buildingAnimator.Animate(building);

            return building;
        }
        
        #endregion
        
        #region Debug

#if UNITY_EDITOR
        public void OnDrawGizmosSelected()
        {
            if (!EditorApplication.isPlaying || !verbose)
            {
                return;
            }

            foreach (KeyValuePair<ChunkIndex, HashSet<int3>> chunkIndexToChunk in ChunkIndexToChunks)
            {
                Gizmos.color = Color.blue;
                Vector3 buildingIndex = BuildingManager.Instance.ChunkWaveFunction[chunkIndexToChunk.Key].Position + Vector3.up;
                Gizmos.DrawWireCube(buildingIndex, BuildingManager.Instance.CellSize * 0.75f);

                foreach (int3 int3 in chunkIndexToChunk.Value)
                {
                    Gizmos.color = Color.magenta;
                    if (!ChunkWaveFunction.Chunks.ContainsKey(int3))
                    {
                        Debug.LogError("Index: " + int3 + " not in Chunk List");
                        continue;
                    }
                    Vector3 districtpos = ChunkWaveFunction.Chunks[int3].Position + ChunkScale.XyZ(0) / 2.0f + Vector3.up;
                    Gizmos.DrawWireCube(districtpos, ChunkScale * 0.75f);
                    Gizmos.DrawLine(Vector3.Lerp(buildingIndex, districtpos, 0.1f), districtpos);
                }
            }
            
            return;
            foreach (QueryChunk chunk in waveFunction.Chunks.Values)
            {
                /*foreach (Cell cell in chunk.Cells)
                {
                    Gizmos.color = !cell.Collapsed ? Color.red : cell.PossiblePrototypes[0].MeshRot.MeshIndex == -1 ? Color.blue : Color.white;
                    Gizmos.DrawWireCube(cell.Position + CellSize / 2.0f, CellSize * 0.75f);
                }*/
                
                Vector3 pos = ChunkWaveUtility.GetPosition(chunk.ChunkIndex, ChunkScale);
                if (queriedChunks.Contains(chunk))
                {
                    Gizmos.color = Color.red;
                }
                else
                {
                    Gizmos.color = ChunkIndexToChunks.Values.Any(x => x.Contains(chunk.ChunkIndex)) ? Color.magenta : Color.white;
                }

                Gizmos.DrawWireCube(pos + ChunkScale / 2.0f, ChunkScale * 0.75f);
            }
        }
#endif
        #endregion

        public ChunkIndex? GetBuildingCell(int3 chunkChunkIndex)
        {
            Vector3 pos = ChunkWaveUtility.GetPosition(chunkChunkIndex, ChunkScale);
            return BuildingManager.Instance.GetIndex(pos + BuildingManager.Instance.CellSize.XyZ(0) / 2.0f);
        }
    }
}
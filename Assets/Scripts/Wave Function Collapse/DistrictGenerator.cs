using System.Collections.Generic;
using Debug = UnityEngine.Debug;
using Cysharp.Threading.Tasks;
using Sirenix.Serialization;
using Sirenix.OdinInspector;
using System.Diagnostics;
using Buildings.District;
using Unity.Mathematics;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;
using System.Linq;
using Buildings;
using Gameplay;
using System;
using Gameplay.Event;

namespace WaveFunctionCollapse
{
    public class DistrictGenerator : SerializedMonoBehaviour, IChunkWaveFunction<QueryChunk>
    {
        public event Action<QueryChunk> OnDistrictChunkRemoved;
        public event Action<QueryChunk> OnDistrictChunkCleared;
        public event Action<ChunkIndex> OnCellCollapsed;
        public event Action OnFinishedGenerating;
        
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

        private readonly Queue<Func<UniTask>> GeneratorActionQueue = new Queue<Func<UniTask>>();

        private BuildingAnimator buildingAnimator;
        private Vector3 offset;

        private bool isUpdatingChunks;
        private bool isRemovingChunks;

        public Vector3 ChunkScale => new Vector3(chunkSize.x * ChunkWaveFunction.CellSize.x, chunkSize.y * ChunkWaveFunction.CellSize.y, chunkSize.z * ChunkWaveFunction.CellSize.z);
        public HashSet<QueryChunk> QueriedChunks { get; } = new HashSet<QueryChunk>();

        public Vector3 CellSize => waveFunction.CellSize;
        public Vector3Int ChunkSize => chunkSize;
        
        public ChunkWaveFunction<QueryChunk> ChunkWaveFunction => waveFunction;
        private bool ShouldAwait => awaitEveryFrame > 0;
        public bool IsGenerating { get; private set; }

        private void OnEnable()
        {
            offset = waveFunction.CellSize.XyZ(0) / 2.0f;
            buildingAnimator = FindAnyObjectByType<BuildingAnimator>();

            waveFunction.Load(this);
            Events.OnBuildingBuilt += UpdateChunksAtBuildables;

            UpdateActions().Forget();
        }
        
        private void OnDisable()
        {
            Events.OnBuildingBuilt -= UpdateChunksAtBuildables;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                IsGenerating = false;
                foreach (QueryChunk chunk in waveFunction.Chunks.Values)
                {
                    chunk.Clear(waveFunction.GameObjectPool);
                    ClearChunkMeshes(chunk.ChunkIndex, false);
                }
                
                foreach (QueryChunk chunk in waveFunction.Chunks.Values)
                {
                    waveFunction.LoadCells(chunk, chunk.PrototypeInfoData);
                }

                Run(waveFunction.Chunks.Values, true).Forget();
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
        
        public void UpdateChunksAtBuildables(ICollection<IBuildable> buildables)
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
            //Debug.Log("Updating Chunks", this);

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
                await Run(overrideChunks.Where(x => !x.IsRemoved).ToList(), true);
            }

            isUpdatingChunks = false;

            void HandleBuildable(IBuildable buildable, HashSet<QueryChunk> overrideChunks, List<Vector3> positions)
            {
                for (int i = 0; i < WaveFunctionUtility.Corners.Length; i++)
                {
                    int2 corner = WaveFunctionUtility.Corners[i];
                    bool isBuildable = buildableCornerData.IsCornerBuildable(buildable.MeshRot, corner, out bool meshIsBuildable);
                    if (!isBuildable && !meshIsBuildable) continue; 

                    Vector3 cornerOffset = new Vector3(corner.x * chunkSize.x * waveFunction.CellSize.x, 0, corner.y * chunkSize.z * waveFunction.CellSize.z) * 0.5f;
                    Vector3 pos = buildable.gameObject.transform.position + cornerOffset - offset;
                    int3 index = ChunkWaveUtility.GetDistrictIndex3(pos, ChunkScale);
                    if (!TryGetBuildingCell(index, out ChunkIndex chunkIndex))
                    {
                        Debug.LogError("Should never get here");
                        return;
                    }
                    
                    if (waveFunction.Chunks.TryGetValue(index, out QueryChunk chunk))
                    {
                        HandleBuiltChunk(chunk, isBuildable, overrideChunks, chunkIndex);
                    }
                    else if (isBuildable)
                    {
                        positions.Add(pos);
                        if (ChunkIndexToChunks.TryGetValue(chunkIndex, out HashSet<int3> list)) list.Add(index);
                        else ChunkIndexToChunks.Add(chunkIndex, new HashSet<int3> { index });
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
                        ClearChunkMeshes(chunk.ChunkIndex, false);
                        overrideChunks.Add(chunk);
                    }
                    else
                    {
                        OnDistrictChunkRemoved?.Invoke(chunk);
                        waveFunction.RemoveChunk(chunk.ChunkIndex, out List<QueryChunk> neighbourChunks);
                        ClearChunkMeshes(chunk.ChunkIndex, false);
                        for (int i = 0; i < neighbourChunks.Count; i++)
                        {
                            ResetNeighbours(overrideChunks, neighbourChunks[i], 1);
                        }

                        if (!ChunkIndexToChunks.TryGetValue(buildIndex, out HashSet<int3> list))
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
                ClearChunkMeshes(adjacentChunk.ChunkIndex, false);
                
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
                ClearChunkMeshes(index, false);
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

            await Run(neighbours, true);
            isRemovingChunks = false;
        }

        public async UniTask Run(ICollection<QueryChunk> chunksToCollapse, bool isNew = false)
        {
            if (IsGenerating)
            {
                await UniTask.WaitWhile(() => IsGenerating);
            }
   
            IsGenerating = true;
            Stopwatch watch = Stopwatch.StartNew();
            int frameCount = 0;

            while (!IsAllCollapsed())
            {
                watch.Start();
                ChunkIndex? index = waveFunction.GetLowestEntropyIndex(chunksToCollapse);
                if (!index.HasValue)
                {
                    Debug.LogError("Could not find lowest entropy index");
                    IsGenerating = false;
                    return;
                }
                PrototypeData chosenPrototype = waveFunction.Collapse(waveFunction[index.Value]);
                SetCell(index.Value, chosenPrototype, isNew, false);

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
            
            QueriedChunks.Clear();
            Place();

            IsGenerating = false;
            OnFinishedGenerating?.Invoke();

            bool IsAllCollapsed()
            {
                foreach (QueryChunk x in chunksToCollapse)
                {
                    if (!x.AllCollapsed && !x.IsClear)
                    {
                        return false;
                    }
                }
                
                return true;
            } 
        }
        
        #region Query & Place

        public void Place()
        {
            Events.OnBuildingBuilt?.Invoke(QuerySpawnedBuildings.Values);
            foreach (QueryChunk chunk in QueriedChunks)
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
            QueriedChunks.Clear();
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
            
            foreach (QueryChunk chunk in QueriedChunks)
            {
                chunk.RevertQuery(SetCell, ChunkWaveFunction.RemoveChunk);
            }
            
            foreach (IBuildable item in QuerySpawnedBuildings.Values)
            {
                item.gameObject.SetActive(false);
            }
            
            QueryChangedData.Clear();
            QuerySpawnedBuildings.Clear();
            QueriedChunks.Clear();
        }
        
        public Dictionary<ChunkIndex, IBuildable> Query(Vector3 position, int height, PrototypeInfoData prototypeInfoData)
        {
            if (IsGenerating || isRemovingChunks || isUpdatingChunks)
            {
                return null;
            }
            
            RevertQuery();

            int2[,] chunksToCollapse = new int2[2, 2];
            for (int i = 0; i < WaveFunctionUtility.Corners.Length; i++)
            {
                int2 corner = WaveFunctionUtility.Corners[i];
                Vector3 cornerOffset = new Vector3(corner.x * chunkSize.x * waveFunction.CellSize.x, 0, corner.y * chunkSize.z * waveFunction.CellSize.z) * 0.5f;
                Vector3 pos = position + cornerOffset;
                int2 index = ChunkWaveUtility.GetDistrictIndex2(pos, ChunkScale);
                chunksToCollapse[corner.x == -1 ? 0 : 1, corner.y == -1 ? 0 : 1] = index;
            }
            
            for (int x = 0; x < chunksToCollapse.GetLength(0); x++)
            for (int z = 0; z < chunksToCollapse.GetLength(1); z++)
            for (int y = 0; y < height; y++)
            {
                int3 index = chunksToCollapse[x, z].XyZ(y);
                if (ChunkWaveFunction.Chunks.TryGetValue(index, out QueryChunk chunkAtIndex))
                {
                    // It's not a district because then it's not valid to query (from the DistrictPlacer)
                    QueriedChunks.Add(chunkAtIndex);
                    QueryChangedData.Add(index, chunkAtIndex.PrototypeInfoData);
                    chunkAtIndex.QueryLoad(prototypeInfoData, CellSize, ChunkWaveFunction.CellStack);

                    ClearChunkMeshes(chunkAtIndex.ChunkIndex, true);   
                }
                else
                {
                    chunkAtIndex = ChunkWaveFunction.LoadChunk(index, ChunkSize, prototypeInfoData);
                    QueriedChunks.Add(chunkAtIndex);
                }
            }

            QueryResetNeighbours(QueriedChunks, 1);

            waveFunction.Propagate();

            IsGenerating = true;
            int tries = 1000;

            while (!AllCollapsed() && tries-- > 0)
            {
                ChunkIndex? index = waveFunction.GetLowestEntropyIndex(QueriedChunks);
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

        private bool AllCollapsed()
        {
            foreach (QueryChunk x in QueriedChunks)
            {
                if (!x.IsClear && !x.AllCollapsed)
                {
                    return false;
                }
            }

            return true;
        }

        private void QueryResetNeighbours(HashSet<QueryChunk> chunks, int depth)
        {
            while (depth >= 0)
            {
                HashSet<QueryChunk> frontier = new HashSet<QueryChunk>();
                foreach (QueryChunk chunk in chunks)
                {
                    for (int i = 0; i < chunk.AdjacentChunks.Length; i++)
                    {
                        IChunk neighbourChunk = chunk.AdjacentChunks[i];
                        if (neighbourChunk is not QueryChunk adjacentChunk
                            || chunks.Contains(adjacentChunk)) continue;

                        frontier.Add(adjacentChunk);
                    }
                }

                foreach (QueryChunk chunk in frontier)
                {
                    chunk.QueryLoad(chunk.PrototypeInfoData, ChunkWaveFunction.CellSize, ChunkWaveFunction.CellStack);
                    ClearChunkMeshes(chunk.ChunkIndex, true);
                }

                chunks.AddRange(frontier);
                depth -= 1;
            }
        }

        public void ClearChunkMeshes(int3 chunkIndex, bool isQuery)
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

            if (!isQuery && ChunkWaveFunction.Chunks.TryGetValue(chunkIndex, out QueryChunk chunk))
            {
                OnDistrictChunkCleared?.Invoke(chunk);
            }
        }

        private void SetCell(ChunkIndex index, PrototypeData chosenPrototype, bool isNew = false, bool query = true)
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
            
            if (isNew)
            {
                OnCellCollapsed?.Invoke(index);
            }
        }

        private IBuildable GenerateMesh(Vector3 position, ChunkIndex index, PrototypeData prototypeData, bool animate = false)
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
                Vector3 buildingIndex = BuildingManager.Instance.ChunkWaveFunction[chunkIndexToChunk.Key].Position + Vector3.up * 0.25f;
                Gizmos.DrawWireCube(buildingIndex, BuildingManager.Instance.CellSize * 0.75f);

                foreach (int3 int3 in chunkIndexToChunk.Value)
                {
                    Gizmos.color = Color.magenta;
                    if (!ChunkWaveFunction.Chunks.ContainsKey(int3))
                    {
                        Debug.LogError("Index: " + int3 + " not in Chunk List");
                        continue;
                    }
                    Vector3 districtpos = ChunkWaveFunction.Chunks[int3].Position + ChunkScale.XyZ(0) / 2.0f + Vector3.up * 0.25f;
                    Gizmos.DrawWireCube(districtpos, ChunkScale * 0.75f);
                    Gizmos.DrawLine(Vector3.Lerp(buildingIndex, districtpos, 0.1f), districtpos);
                }
            }
            
            /*foreach (QueryChunk chunk in waveFunction.Chunks.Values)
            {
                /*foreach (Cell cell in chunk.Cells)
                {
                    Gizmos.color = !cell.Collapsed ? Color.red : cell.PossiblePrototypes[0].MeshRot.MeshIndex == -1 ? Color.blue : Color.white;
                    Gizmos.DrawWireCube(cell.Position + CellSize / 2.0f, CellSize * 0.75f);
                }#1#
                
                Vector3 pos = ChunkWaveUtility.GetPosition(chunk.ChunkIndex, ChunkScale);
                if (QueriedChunks.Contains(chunk))
                {
                    Gizmos.color = Color.red;
                }
                else
                {
                    Gizmos.color = ChunkIndexToChunks.Values.Any(x => x.Contains(chunk.ChunkIndex)) ? Color.magenta : Color.white;
                }

                Gizmos.DrawWireCube(pos + ChunkScale / 2.0f, ChunkScale * 0.75f);
            }*/
        }
#endif
        #endregion

        public bool TryGetBuildingCell(int3 districtChunkIndex, out ChunkIndex index)
        {
            Vector3 pos = ChunkWaveUtility.GetPosition(districtChunkIndex, ChunkScale);
            return BuildingManager.Instance.TryGetIndex(pos + BuildingManager.Instance.CellSize.XyZ(0) / 2.0f, out index);
        }
    }
}
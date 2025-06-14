using System.Collections.Generic;
using Debug = UnityEngine.Debug;
using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using Unity.Mathematics;
using UnityEngine;
using System.Linq;
using UnityEditor;
using System;
using Buildings;

public class BuildingManager : Singleton<BuildingManager>, IQueryWaveFunction
{
    public event Action<QueryMarchedChunk> OnLoaded;

    [Title("Wave Function")]
    [SerializeField]
    private ChunkWaveFunction<QueryMarchedChunk> waveFunction;

    [Title("Prototypes")]
    [SerializeField]
    private PrototypeInfoData townPrototypeInfo;

    [SerializeField]
    private ProtoypeMeshes prototypeMeshes;
    
    [Title("Mesh")]
    [SerializeField]
    private Building buildingPrefab;

    [SerializeField]
    private BuildableCornerData cellBuildableCornerData;

    [Title("Debug")]
    [SerializeField]
    private PooledMonoBehaviour unableToPlacePrefab;
    
    public Dictionary<ChunkIndex, IBuildable> QuerySpawnedBuildings { get; } = new Dictionary<ChunkIndex, IBuildable>();
    public Dictionary<ChunkIndex, IBuildable> SpawnedMeshes  { get; } = new Dictionary<ChunkIndex, IBuildable>();

    private HashSet<QueryMarchedChunk> queriedChunks = new HashSet<QueryMarchedChunk>();
    private IEnumerable<ChunkIndex> queryBuiltIndexes;

    private BuildingAnimator buildingAnimator;
    private GroundGenerator groundGenerator;
    private Vector3? gridScale;
    
    public int3 ChunkSize => new int3(Mathf.FloorToInt(groundGenerator.ChunkSize.x / waveFunction.CellSize.x), 1, Mathf.FloorToInt(groundGenerator.ChunkSize.z / waveFunction.CellSize.z));
    public ChunkWaveFunction<QueryMarchedChunk> ChunkWaveFunction => waveFunction;
    public IEnumerable<ChunkIndex> QueryBuiltIndexes => queryBuiltIndexes;
    public PrototypeInfoData PrototypeInfo => townPrototypeInfo;
    public Vector3 ChunkScale => groundGenerator.ChunkScale;
    public bool IsGenerating { get; private set; }
    
    public Vector3 CellSize
    {
        get
        {
            gridScale ??= waveFunction.CellSize.MultiplyByAxis(groundGenerator.ChunkWaveFunction.CellSize);
            return gridScale.Value;
        }
    }
    
    private void OnEnable()
    {
        groundGenerator = FindFirstObjectByType<GroundGenerator>();
        buildingAnimator = GetComponent<BuildingAnimator>();

        groundGenerator.OnChunkGenerated += LoadCells;
        Events.OnBuiltIndexDestroyed += RemoveBuiltIndex;
    }
    
    private void OnDisable()
    {
        groundGenerator.OnChunkGenerated -= LoadCells;
        Events.OnBuiltIndexDestroyed -= RemoveBuiltIndex;
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            int total = 0;
            int queryChangedCount = 0;
            foreach (QueryMarchedChunk chunk in waveFunction.Chunks.Values)
            {
                queryChangedCount += chunk.QueryChangedCells.Count;
                foreach (Cell cell in chunk.Cells)
                {
                    if (cell.Collapsed)
                    {
                        total++;
                    }
                }
            }
            Debug.Log($"Collapsed Count: {total}");
            Debug.Log($"QueryChanged Count: {queryChangedCount}");
        }
    }

    private void LoadCells(Chunk chunk)
    {
        int3 index = chunk.ChunkIndex;
        QueryMarchedChunk queryChunk = new QueryMarchedChunk().Construct(
            Mathf.FloorToInt(chunk.Width / waveFunction.CellSize.x),
            1,
            Mathf.FloorToInt(chunk.Depth / waveFunction.CellSize.z),
            index,
            chunk.Position,
            waveFunction.GetAdjacentChunks(index).ToArray<IChunk>(),
            false) as QueryMarchedChunk;
        
        queryChunk.Handler = this;
        Vector3 offset = new Vector3(CellSize.x / 2.0f, 0, CellSize.z / 2.0f);
        queryChunk.LoadCells(townPrototypeInfo, CellSize, chunk, offset, cellBuildableCornerData);
        waveFunction.LoadChunk(index, queryChunk);
        
        OnLoaded?.Invoke(queryChunk);
    }

    public void RemoveBuiltIndex(ChunkIndex builtIndex)
    {
        if (!waveFunction.Chunks[builtIndex.Index].BuiltCells[builtIndex.CellIndex.x, builtIndex.CellIndex.y, builtIndex.CellIndex.z])
        {
            return; // Index is not built
        }
        
        // Reset Indexes
        waveFunction.Chunks[builtIndex.Index].BuiltCells[builtIndex.CellIndex.x, builtIndex.CellIndex.y, builtIndex.CellIndex.z] = false;
        List<ChunkIndex> chunkIndexes = this.GetCellsSurroundingMarchedIndex(builtIndex);
        
        // Get neighbours
        HashSet<ChunkIndex> cellsToUpdate = new HashSet<ChunkIndex>(chunkIndexes);
        int3 gridSize = waveFunction.Chunks[chunkIndexes[0].Index].ChunkSize;
        for (int i = 0; i < chunkIndexes.Count; i++)
        {
            GetNeighbours(chunkIndexes[i], 1);
        }
        
        queriedChunks = this.GetChunks(cellsToUpdate);
        this.MakeBuildable(cellsToUpdate, PrototypeInfo);
        
        waveFunction.Propagate();

        int tries = 1000;
        IsGenerating = true;
        while (cellsToUpdate.Any(x => !waveFunction[x].Collapsed) && tries-- > 0)
        {
            ChunkIndex index = waveFunction.GetLowestEntropyIndex(cellsToUpdate);
            PrototypeData chosenPrototype = waveFunction.Collapse(waveFunction[index]);
            if (chosenPrototype.MeshRot.MeshIndex == -1)
            {
                Cell cell = waveFunction[index];
                cell.PossiblePrototypes = new List<PrototypeData> { PrototypeData.Empty };
                cell.SetDirty();
                waveFunction[index] = cell;
                cellsToUpdate.Remove(index);
                QuerySpawnedBuildings.Remove(index);
            }
            else
            {
                this.SetCell(index, chosenPrototype);
            }
            
            waveFunction.Propagate();
        }
        IsGenerating = false;

        if (tries <= 0)
        {
            RevertQuery();
            return;
        }
        
        Place();

        void GetNeighbours(ChunkIndex chunkIndex, int depth)
        {
            List<ChunkIndex> neighbours = ChunkWaveUtility.GetNeighbouringChunkIndexes(chunkIndex, gridSize.x, gridSize.z);
            for (int i = 0; i < neighbours.Count; i++)
            {
                if (cellsToUpdate.Contains(neighbours[i])
                    || !waveFunction.Chunks.TryGetValue(neighbours[i].Index, out QueryMarchedChunk neihbourChunk)
                    || !neihbourChunk[neighbours[i].CellIndex].Collapsed
                    || !IsBuildable(neihbourChunk, neighbours[i].CellIndex)) continue;
                
                cellsToUpdate.Add(neighbours[i]);

                if (depth <= 0) continue;
                GetNeighbours(neighbours[i], depth - 1);
            }
        }
    }

    #region Query & Place

    public void Place()
    {
        Events.OnBuildingBuilt?.Invoke(QuerySpawnedBuildings.Values);
        
        if (queryBuiltIndexes != null)
        {
            Events.OnBuiltIndexBuilt?.Invoke(queryBuiltIndexes);
            queryBuiltIndexes = null;   
        }
        
        foreach (QueryMarchedChunk chunk in queriedChunks)
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
    }

    public void RevertQuery()
    {
        queryBuiltIndexes = null;
        if (QuerySpawnedBuildings.Count == 0)
        {
            return;
        }
        
        foreach (QueryMarchedChunk chunk in queriedChunks)
        {
            chunk.RevertQuery();
        }
        
        foreach (IBuildable item in QuerySpawnedBuildings.Values)
        {
            item.gameObject.SetActive(false);
        }
        QuerySpawnedBuildings.Clear();
    }

    public Dictionary<ChunkIndex, IBuildable> Query(ChunkIndex queryIndex)
    {
        List<ChunkIndex> cellsToCollapse = this.GetCellsToCollapse(queryIndex);
        return Query(cellsToCollapse, new List<ChunkIndex> { queryIndex });
    }
    
    public Dictionary<ChunkIndex, IBuildable> Query(List<ChunkIndex> cellsToCollapse, IEnumerable<ChunkIndex> builtIndexes)
    {
        if (IsGenerating)
        {
            return new Dictionary<ChunkIndex, IBuildable>();
        }
        
        RevertQuery();

        if (cellsToCollapse.Count <= 0) return QuerySpawnedBuildings;
        queriedChunks = this.GetChunks(cellsToCollapse);
        queryBuiltIndexes = builtIndexes;

        foreach (ChunkIndex builtIndex in queryBuiltIndexes)
        {
            waveFunction.Chunks[builtIndex.Index].SetBuiltCells(builtIndex.CellIndex);
        }
        
        this.MakeBuildable(cellsToCollapse, PrototypeInfo);

        waveFunction.Propagate();

        IsGenerating = true;
        int tries = 1000;
        while (cellsToCollapse.Any(x => !waveFunction[x].Collapsed) && tries-- > 0)
        {
            ChunkIndex index = waveFunction.GetLowestEntropyIndex(cellsToCollapse);
            PrototypeData chosenPrototype = waveFunction.Collapse(waveFunction[index]);
            this.SetCell(index, chosenPrototype);

            waveFunction.Propagate();
        }
        IsGenerating = false;

        if (tries <= 0)
        {
            Debug.LogError("Ran out of attempts to collapse");
        }

        return QuerySpawnedBuildings;
    }

    public IBuildable GenerateMesh(Vector3 position, ChunkIndex index, PrototypeData prototypeData, bool animate = false)
    {
        Building building = buildingPrefab.GetAtPosAndRot<Building>(position, Quaternion.Euler(0, 90 * prototypeData.MeshRot.Rot, 0)); 

        building.Setup(prototypeData, index, waveFunction.CellSize);

        if (animate) buildingAnimator.Animate(building);

        return building;
    }
    
    #endregion

    #region Utility
    
    public bool IsBuildable(ChunkIndex chunkIndex)
    {
        return IsBuildable(waveFunction.Chunks[chunkIndex.Index], chunkIndex.CellIndex);
    }
    
    public bool IsBuildable(QueryMarchedChunk chunk, int3 index)
    {
        return (chunk.GroundTypes[index.x, index.y, index.z] & (GroundType.Grass | GroundType.Crystal)) != 0;
    }
    
    #endregion

    #region Debug

    private void OnDrawGizmosSelected()
    {
#if UNITY_EDITOR
        if (!EditorApplication.isPlaying) return;
#endif
        
        if (waveFunction.Chunks == null || waveFunction.Chunks.Count == 0)
        {
            return;
        }

        foreach (QueryMarchedChunk chunk in waveFunction.Chunks.Values)
        {
            for (int y = 0; y < chunk.Cells.GetLength(2); y++)
            {
                for (int x = 0; x < chunk.Cells.GetLength(0); x++)
                {
                    Vector3 pos = chunk.Cells[x, 0, y].Position;
                    Gizmos.color = chunk.BuiltCells[x, 0, y] 
                        ? Color.magenta 
                        : chunk.Cells[x, 0, y].Collapsed 
                            ? Color.blue 
                            : Color.white;
                    Gizmos.DrawWireCube(pos, CellSize * 0.9f);
                }
            }
        }
        
    }

    #endregion
}
using System.Collections.Generic;
using Debug = UnityEngine.Debug;
using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using Unity.Mathematics;
using UnityEngine;
using System.Linq;
using UnityEditor;
using Buildings;
using System;

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
    
    [Title("Keys")]
    [SerializeField]
    private PrototypeKeyData keyData;

    [Title("Mesh")]
    [SerializeField]
    private Building buildingPrefab;

    [SerializeField]
    private Path pathPrefab;

    [SerializeField]
    private BuildableCornerData cellBuildableCornerData;

    [Title("Debug")]
    [SerializeField]
    private PooledMonoBehaviour unableToPlacePrefab;

    public ChunkWaveFunction<QueryMarchedChunk> ChunkWaveFunction => waveFunction;
    
    public Vector3 ChunkScale => groundGenerator.ChunkScale;

    public Vector3 GridScale
    {
        get
        {
            gridScale ??= waveFunction.GridScale.MultiplyByAxis(groundGenerator.ChunkWaveFunction.GridScale);
            return gridScale.Value;
        }
    }
    
    public bool IsGenerating { get; private set; }
    
    private readonly Dictionary<ChunkIndex, IBuildable> querySpawnedBuildings = new Dictionary<ChunkIndex, IBuildable>();
    private readonly Dictionary<ChunkIndex, IBuildable> spawnedMeshes = new Dictionary<ChunkIndex, IBuildable>();

    private List<QueryMarchedChunk> queriedChunks = new List<QueryMarchedChunk>();
    private HashSet<short> allowedKeys;

    private BuildingAnimator buildingAnimator;
    private GroundGenerator groundGenerator;
    private ChunkIndex queryIndex;
    private Vector3? gridScale;
    
    private void OnEnable()
    {
        groundGenerator = FindFirstObjectByType<GroundGenerator>();
        buildingAnimator = GetComponent<BuildingAnimator>();

        groundGenerator.OnChunkGenerated += LoadCells;
        Events.OnBuildingDestroyed += OnBuildingDestroyed;
        Events.OnBuildingRepaired += OnBuildingRepaired;
    }
    
    private void OnDisable()
    {
        groundGenerator.OnChunkGenerated -= LoadCells;
        Events.OnBuildingDestroyed += OnBuildingDestroyed;
        Events.OnBuildingRepaired += OnBuildingRepaired;
    }

    [Button]
    public void ShowCollapsedTiles()
    {
        foreach (var item in waveFunction.Chunks.Values)
        {
            foreach (var cell in item.Cells)
            {
                if (cell.Collapsed)
                {
                    unableToPlacePrefab.GetAtPosAndRot<PooledMonoBehaviour>(item.Position, Quaternion.identity);
                }
            }
            
        }
    }

    #region Loading

    private void LoadCells(Chunk chunk)
    {
        int3 index = chunk.ChunkIndex;
        QueryMarchedChunk queryChunk = new QueryMarchedChunk().Construct(
            Mathf.FloorToInt(chunk.Width / waveFunction.GridScale.x),
            1,
            Mathf.FloorToInt(chunk.Depth / waveFunction.GridScale.z),
            index,
            chunk.Position,
            waveFunction.GetAdjacentChunks(index).ToArray<IChunk>(),
            false) as QueryMarchedChunk;
        
        queryChunk.Handler = this;
        Vector3 offset = new Vector3(GridScale.x / 2.0f, 0, GridScale.z / 2.0f);
        queryChunk.LoadCells(townPrototypeInfo, GridScale, chunk, cellBuildableCornerData, offset);
        waveFunction.LoadChunk(index, queryChunk);
        
        OnLoaded?.Invoke(queryChunk);
    }

    #endregion

    #region Events

    private void OnBuildingRepaired(Building building)
    {
        waveFunction[building.Index] = new Cell(true, waveFunction[building.Index].Position, waveFunction[building.Index].PossiblePrototypes);
    }

    private void OnBuildingDestroyed(Building building)
    {
        return;
        //this[building.Index] = new Cell(false, this[building.Index].Position, prototypes);
        //List<ChunkIndex> cellsToUpdate = new List<ChunkIndex>();
        //for (int i = 0; i < WaveFunctionUtility.NeighbourDirections.Length; i++)
        //{
        //    ChunkIndex index =  new ChunkIndex(building.Index.Index, new int3(WaveFunctionUtility.NeighbourDirections[i].x + building.Index.CellIndex.x, 0, WaveFunctionUtility.NeighbourDirections[i].y + building.Index.CellIndex.z) );
        //    if (waveFunction[index].Collapsed)
        //    {
        //        cellsToUpdate.Add(index);
        //    }
        //}

        //allowedKeys = keyData.BuildingKeys;
        //MakeBuildable(cellsToUpdate);
        //
        //Propagate();

        //int tries = 1000;
        //while (cellsToCollapse.Any(x => !waveFunction[x].Collapsed) && tries-- > 0)
        //{
        //    Iterate();
        //}

        //if (tries <= 0)
        //{
        //    RevertQuery();
        //    return;
        //}
        //Place();
    }

    #endregion

    #region Query & Place

    public void Place()
    {
        Events.OnBuildingBuilt?.Invoke(querySpawnedBuildings.Values);
        foreach (QueryMarchedChunk chunk in queriedChunks)
        {
            chunk.Place();
        }
        
        foreach (KeyValuePair<ChunkIndex, IBuildable> item in querySpawnedBuildings)
        {
            spawnedMeshes.Add(item.Key, item.Value);
        }

        foreach (IBuildable item in querySpawnedBuildings.Values)
        {
            item.ToggleIsBuildableVisual(false);
        }
        
        querySpawnedBuildings.Clear();
    }

    public void RevertQuery()
    {
        foreach (QueryMarchedChunk chunk in queriedChunks)
        {
            chunk.RevertQuery();
        }
        
        foreach (IBuildable item in querySpawnedBuildings.Values)
        {
            item.gameObject.SetActive(false);
        }
        querySpawnedBuildings.Clear();
    }

    public Dictionary<ChunkIndex, IBuildable> Query(ChunkIndex queryIndex, BuildingType buildingType)
    {
        if (querySpawnedBuildings.Count > 0)
        {
            RevertQuery();
        }

        List<ChunkIndex> cellsToCollapse = GetCellsToCollapse(queryIndex);
        if (cellsToCollapse.Count <= 0) return querySpawnedBuildings;
        
        allowedKeys = buildingType switch
        {
            BuildingType.Building => keyData.BuildingKeys,
            BuildingType.Path => keyData.PathKeys,
            _ => allowedKeys
        };
        
        queriedChunks = GetChunks(cellsToCollapse);
        waveFunction.Chunks[queryIndex.Index].SetBuiltCells(queryIndex.CellIndex);
        MakeBuildable(cellsToCollapse);

        waveFunction.Propagate(allowedKeys);

        IsGenerating = true;
        int tries = 1000;
        while (cellsToCollapse.Any(x => !waveFunction[x].Collapsed) && tries-- > 0)
        {
            ChunkIndex index = waveFunction.GetLowestEntropyIndex(cellsToCollapse);
            PrototypeData chosenPrototype = waveFunction.Collapse(waveFunction[index]);
            SetCell(index, chosenPrototype, waveFunction.Chunks[index.Index].QueryCollapsedAir);

            waveFunction.Propagate(allowedKeys);
        }
        IsGenerating = false;

        if (tries <= 0)
        {
            Debug.LogError("Ran out of attempts to collapse");
        }

        return querySpawnedBuildings;
    }

    private List<QueryMarchedChunk> GetChunks(List<ChunkIndex> cellsToCollapse)
    {
        List<QueryMarchedChunk> chunks = new List<QueryMarchedChunk> {waveFunction.Chunks[cellsToCollapse[0].Index]};
        for (int i = 1; i < cellsToCollapse.Count; i++)
        {
            QueryMarchedChunk chunk = waveFunction.Chunks[cellsToCollapse[i].Index];
            if (!chunks.Contains(chunk))
            {
                chunks.Add(chunk);
            }
        }

        return chunks;
    }

    private void MakeBuildable(List<ChunkIndex> cellsToCollapse) 
    {
        for (int i = 0; i < cellsToCollapse.Count; i++)
        {
            ChunkIndex index = cellsToCollapse[i];
            QueryMarchedChunk chunk = waveFunction.Chunks[index.Index];
            if (!waveFunction[index].Buildable) continue;

            int marchedIndex = GetMarchIndex(index);
            chunk.QueryChangedCells.Add((index.CellIndex, chunk[index.CellIndex]));
                
            chunk[index.CellIndex] = new Cell(false, 
                chunk[index.CellIndex].Position, 
                new List<PrototypeData>(townPrototypeInfo.MarchingTable[marchedIndex]));

            chunk.GetAdjacentCells(index.CellIndex, out _).ForEach(x => waveFunction.CellStack.Push(x));

            if (spawnedMeshes.TryGetValue(index, out IBuildable buildable))
            {
                buildable.gameObject.SetActive(false);
                spawnedMeshes.Remove(index);
            }
        }
    }
    
    private int GetMarchIndex(ChunkIndex index)
    {
        int marchedIndex = 0;
        Vector3 pos = waveFunction[index].Position + GridScale / 2.0f;
        for (int i = 0; i < 4; i++)
        {
            Vector3 marchPos = pos + new Vector3(WaveFunctionUtility.MarchDirections[i].x * GridScale.x, 0, WaveFunctionUtility.MarchDirections[i].y * GridScale.z);
            ChunkIndex? chunk = GetIndex(marchPos);
            if (chunk.HasValue && waveFunction.Chunks[chunk.Value.Index].BuiltCells[chunk.Value.CellIndex.x, chunk.Value.CellIndex.y, chunk.Value.CellIndex.z])
            {
                marchedIndex += (int)Mathf.Pow(2, i);
            }
        }
            
        return marchedIndex;
    }

    public List<ChunkIndex> GetCellsToCollapse(ChunkIndex queryIndex)
    {
        return GetSurroundingCells(waveFunction[queryIndex].Position + new Vector3(GridScale.x + 0.1f, 0, GridScale.z + 0.1f));
    }

    private List<ChunkIndex> GetSurroundingCells(Vector3 queryPosition)
    {
        List<ChunkIndex> surrounding = new List<ChunkIndex>();
        for (int x = -1; x <= 1; x += 2)
        {
            for (int z = -1; z <= 1; z += 2)
            {
                ChunkIndex? index = GetIndex(queryPosition + new Vector3(waveFunction.GridScale.x * x, 0, z * waveFunction.GridScale.z));
                if (index.HasValue) 
                {
                    surrounding.Add(index.Value);
                }
            }
        }

        return surrounding;
    }
    
    public int3? GetIndex(Vector3 pos, IChunk chunk)
    {
        pos -= chunk.Position;
        int3 index = new int3(Math.GetMultiple(pos.x, GridScale.x), 0, Math.GetMultiple(pos.z, GridScale.z));
        if (chunk.Cells.IsInBounds(index))
        {
            return index;
        }

        return null;
    }
    
    public ChunkIndex? GetIndex(Vector3 pos)
    {
        foreach (QueryMarchedChunk chunk in waveFunction.Chunks.Values) 
        {
            if (chunk.ContainsPoint(pos, GridScale))
            {
                int3? cellIndex = GetIndex(pos, chunk);
                if (cellIndex.HasValue)
                {
                    return new ChunkIndex(chunk.ChunkIndex, cellIndex.Value);
                }
                
                return null;
            }
        }

        return null;
    }

    #endregion

    #region Set Cell
    
    public void SetCell(ChunkIndex index, PrototypeData chosenPrototype, List<int3> queryCollapsedAir, bool query = true)
    {
        waveFunction[index] = new Cell(true, waveFunction[index].Position, new List<PrototypeData> { chosenPrototype });
        waveFunction.CellStack.Push(index);

        IBuildable spawned = GenerateMesh(waveFunction[index].Position, chosenPrototype);
        if (spawned == null) 
        {
            queryCollapsedAir.Add(index.CellIndex);
            return;
        }

        if (query)
        {
            querySpawnedBuildings.Add(index, spawned);
            return;
        }

        spawned.ToggleIsBuildableVisual(false);
        spawnedMeshes[index] = spawned;
    }
    
    private IBuildable GenerateMesh(Vector3 position, PrototypeData prototypeData, bool animate = false)
    {
        bool isPath = prototypeData.MeshRot.MeshIndex != -1 && prototypeMeshes[prototypeData.MeshRot.MeshIndex].name.Contains("Path");
        IBuildable building = isPath // Not my best work, but can't use currentBuildingType
            ? pathPrefab.GetAtPosAndRot<Path>(position, Quaternion.Euler(0, 90 * prototypeData.MeshRot.Rot, 0))
            : buildingPrefab.GetAtPosAndRot<Building>(position, Quaternion.Euler(0, 90 * prototypeData.MeshRot.Rot, 0)); 

        building.Setup(prototypeData, waveFunction.GridScale);

        if (animate) buildingAnimator.Animate(building);

        return building;
    }
    
    #endregion

    #region API

    public Vector3 GetPos(ChunkIndex index)
    {
        return waveFunction[index].Position;
    }
    
    #endregion
    
    #region Debug

    private void OnDrawGizmosSelected()
    {
        if (!EditorApplication.isPlaying || waveFunction.Chunks == null || waveFunction.Chunks.Count == 0)
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
                    Gizmos.color = !chunk.BuiltCells[x, 0, y] ? Color.white : Color.magenta;
                    Gizmos.DrawWireCube(pos, GridScale * 0.9f);
                }
            }
        }
        
    }

    #endregion
}
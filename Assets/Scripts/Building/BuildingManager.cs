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
    
    private readonly Dictionary<ChunkIndex, IBuildable> querySpawnedBuildings = new Dictionary<ChunkIndex, IBuildable>();
    private readonly Dictionary<ChunkIndex, IBuildable> spawnedMeshes = new Dictionary<ChunkIndex, IBuildable>();

    private HashSet<short> allowedKeys;

    private Vector3? gridScale;
    private BuildingAnimator buildingAnimator;
    private GroundGenerator groundGenerator;
    private QueryMarchedChunk queriedChunk;
    private ChunkIndex queryIndex;
    
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
        
        queriedChunk.Place();
        
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
        queriedChunk?.RevertQuery();
        
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

        List<int3> cellsToCollapse = GetCellsToCollapse(queryIndex);
        if (cellsToCollapse.Count <= 0) return querySpawnedBuildings;
        
        allowedKeys = buildingType switch
        {
            BuildingType.Building => keyData.BuildingKeys,
            BuildingType.Path => keyData.PathKeys,
            _ => allowedKeys
        };
        
        queriedChunk = waveFunction.Chunks[queryIndex.Index];
        queriedChunk.Query(queryIndex.CellIndex);
        MakeBuildable(cellsToCollapse, queriedChunk);

        waveFunction.Propagate(allowedKeys);

        int tries = 1000;
        while (cellsToCollapse.Any(x => !queriedChunk[x].Collapsed) && tries-- > 0)
        {
            ChunkIndex index = waveFunction.GetLowestEntropyIndex(cellsToCollapse, queriedChunk);
            PrototypeData chosenPrototype = waveFunction.Collapse(waveFunction[index]);
            SetCell(index, chosenPrototype, queriedChunk.QueryCollapsedAir);

            waveFunction.Propagate(allowedKeys);
        }

        if (tries <= 0)
        {
            Debug.LogError("Ran out of attempts to collapse");
        }

        return querySpawnedBuildings;
    }
    
    
    private void MakeBuildable(List<int3> cellsToCollapse, QueryMarchedChunk chunk) 
    {
        for (int i = 0; i < cellsToCollapse.Count; i++)
        {
            int3 index = cellsToCollapse[i];
            if (!chunk[index].Buildable) continue;

            int marchedIndex = GetMarchIndex(index, chunk);
            chunk.QueryChangedCells.Add((index, chunk[index]));
                
            chunk[index] = new Cell(false, 
                chunk[index].Position, 
                new List<PrototypeData>(townPrototypeInfo.MarchingTable[marchedIndex]));

            chunk.GetAdjacentCells(index, out _).ForEach(x => waveFunction.CellStack.Push(x));

            ChunkIndex chunkIndex = new ChunkIndex(chunk.ChunkIndex, index);
            if (spawnedMeshes.TryGetValue(chunkIndex, out IBuildable buildable))
            {
                buildable.gameObject.SetActive(false);
                spawnedMeshes.Remove(chunkIndex);
            }
        }
    }
    
    private int GetMarchIndex(int3 index, QueryMarchedChunk chunk)
    {
        int marchedIndex = 0;
        for (int i = 0; i < 4; i++)
        {
            int3 marchIndex = new int3(index.x + WaveFunctionUtility.MarchDirections[i].x, index.y, index.z + WaveFunctionUtility.MarchDirections[i].y);
            if (chunk.BuiltCells.IsInBounds(marchIndex) && chunk.BuiltCells[marchIndex.x, marchIndex.y, marchIndex.z])
            {
                marchedIndex += (int)Mathf.Pow(2, i);
            }
        }
            
        return marchedIndex;
    }

    public List<int3> GetCellsToCollapse(ChunkIndex queryIndex)
    {
        queryIndex = new ChunkIndex(queryIndex.Index, queryIndex.CellIndex + new int3(1, 0, 1));
        return GetSurroundingCells(waveFunction[queryIndex].Position + new Vector3(0.1f, 0, 0.1f), waveFunction.Chunks[queryIndex.Index]);
    }

    private List<int3> GetSurroundingCells(Vector3 queryPosition, QueryMarchedChunk chunk)
    {
        List<int3> surrounding = new List<int3>();

        for (int x = -1; x <= 1; x += 2)
        {
            for (int z = -1; z <= 1; z += 2)
            {
                int3? index = GetIndex(queryPosition + new Vector3(waveFunction.GridScale.x * x, 0, z * waveFunction.GridScale.z), chunk);
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
    
    private IBuildable GenerateMesh(Vector3 position, PrototypeData prototypeData, bool animate = true)
    {
        IBuildable building;
        if (prototypeData.MeshRot.Mesh != null && prototypeData.MeshRot.Mesh.name.Contains("Path")) // Not my best work
        {
            building = pathPrefab.GetAtPosAndRot<Path>(position, Quaternion.Euler(0, 90 * prototypeData.MeshRot.Rot, 0));
        }
        else
        {
            building = buildingPrefab.GetAtPosAndRot<Building>(position, Quaternion.Euler(0, 90 * prototypeData.MeshRot.Rot, 0));
        }

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
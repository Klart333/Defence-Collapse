using System.Collections.Generic;
using Debug = UnityEngine.Debug;
using UnityEngine.Serialization;
using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using Unity.Collections;
using Unity.Mathematics;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Buildings;
using System;

public class BuildingManager : Singleton<BuildingManager> 
{
    public event Action OnLoaded;

    [Title("Cells")]
    [SerializeField]
    private float cellSize = 1f; // Needs to be an exponent of 2, probably

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

    [FormerlySerializedAs("cellBuildableUtility")]
    [SerializeField]
    private BuildableCornerData cellBuildableCornerData;

    [Title("Debug")]
    [SerializeField]
    private PooledMonoBehaviour unableToPlacePrefab;

    [SerializeField]
    private bool DebugPropagate;

    [SerializeField, ShowIf(nameof(DebugPropagate)), Range(1, 1000)]
    private int Speed;

    private bool[,] cellsBuilt;

    public Cell this[int2 index]
    {
        get => Cells[index.x, index.y];
        private set => Cells[index.x, index.y] = value;
    }

    private readonly List<Vector2Int> marchDirections = new List<Vector2Int>
    {
        new Vector2Int(-1, -1),
        new Vector2Int(0, -1),
        new Vector2Int(0, 0), 
        new Vector2Int(-1, 0),
    };
    
    private readonly Dictionary<int2, IBuildable> querySpawnedBuildings = new Dictionary<int2, IBuildable>();
    private readonly Dictionary<int2, IBuildable> spawnedMeshes = new Dictionary<int2, IBuildable>();
    private readonly List<(int2, Cell)> queryChangedCells = new List<(int2, Cell)>();
    private readonly List<int2> queryCollapsedAir = new List<int2>();
    private readonly Stack<int2> cellStack = new Stack<int2>();
    
    private List<PrototypeData> prototypes = new List<PrototypeData>();
    private List<int2> cellsToCollapse = new List<int2>();
    private List<PrototypeData> unbuildablePrototypeList;
    private HashSet<short> allowedKeys;

    private GroundGenerator groundGenerator;
    private BuildingAnimator buildingAnimator;
    private PrototypeData emptyPrototype;
    private PrototypeData unbuildablePrototype;
    private int2 queryIndex;

    private BuildingType currentBuildingType;
    
    private bool shouldRemoveQueryIndex;
        
    public Cell[,] Cells { get; private set; }
    public float CellSize => cellSize;

    private void OnEnable()
    {
        groundGenerator = FindFirstObjectByType<GroundGenerator>();
        buildingAnimator = GetComponent<BuildingAnimator>();

        groundGenerator.OnChunkGenerated += Load;
        Events.OnBuildingDestroyed += OnBuildingDestroyed;
        Events.OnBuildingRepaired += OnBuildingRepaired;
    }
    
    private void OnDisable()
    {
        groundGenerator.OnChunkGenerated -= Load;
        Events.OnBuildingDestroyed += OnBuildingDestroyed;
        Events.OnBuildingRepaired += OnBuildingRepaired;
    }

    [Button]
    public void ShowCollapsedTiles()
    {
        foreach (var item in Cells)
        {
            if (item.Collapsed)
            {
                 unableToPlacePrefab.GetAtPosAndRot<PooledMonoBehaviour>(item.Position, Quaternion.identity);
            }
        }
    }

    #region Loading

    private void Load(Chunk chunk)
    {
        if (!LoadPrototypeData())
        {
            Debug.LogError("No prototype data found");
            return;
        }

        LoadCells(chunk);

        OnLoaded?.Invoke();
    }

    private void LoadCells(Chunk chunk)
    {
        Cells = new Cell[Mathf.RoundToInt(chunk.width / cellSize), Mathf.RoundToInt(chunk.depth / cellSize)];
        cellsBuilt = new bool[Mathf.RoundToInt(chunk.width / cellSize), Mathf.RoundToInt(chunk.depth / cellSize)];
        emptyPrototype = new PrototypeData(new MeshWithRotation(null, 0), -1, -1, -1, -1, -1, -1, 1, Array.Empty<int>());
        unbuildablePrototype = new PrototypeData(new MeshWithRotation(null, 0), -1, -1, -1, -1, -1, -1, 0, Array.Empty<int>());
        unbuildablePrototypeList = new List<PrototypeData> { unbuildablePrototype };

        for (int y = 0; y < Cells.GetLength(1); y++)
        {
            for (int x = 0; x < Cells.GetLength(0); x++)
            {
                Vector3 pos = new Vector3(x * cellSize * groundGenerator.ChunkWaveFunction.GridScale.x, 0, y * cellSize * groundGenerator.ChunkWaveFunction.GridScale.z) - new Vector3(cellSize * groundGenerator.ChunkWaveFunction.GridScale.x, 0, cellSize * groundGenerator.ChunkWaveFunction.GridScale.z) / 2.0f;
                Cells[x, y] = new Cell(false, pos + transform.position, new List<PrototypeData> { emptyPrototype });
            }
        }

        for (int y = 0; y < Cells.GetLength(1); y++)
        {
            for (int x = 0; x < Cells.GetLength(0); x++)
            {
                int2 cellIndex = new int2(x, y);
                int2 gridIndex = new int2(Mathf.FloorToInt(x * cellSize), Mathf.FloorToInt(y * cellSize));
                SetCellDependingOnGround(cellIndex, gridIndex); 
            }
        }

        return;
        
        void SetCellDependingOnGround(int2 cellIndex, int2 gridIndex)
        {
            Vector3 cellPosition = Cells[cellIndex.x, cellIndex.y].Position;
            if (gridIndex.y == Cells.GetLength(1) - 1) // Just put air at the top
            {
                Cells[cellIndex.x, cellIndex.y] = new Cell(true, cellPosition, unbuildablePrototypeList, false);
                return;
            }

            Cell groundCell = chunk.Cells[gridIndex.x, 0, gridIndex.y];
            Vector2Int corner = new Vector2Int((int)Mathf.Sign(groundCell.Position.x - cellPosition.x), (int)Mathf.Sign(groundCell.Position.z - cellPosition.z));

            if (!cellBuildableCornerData.IsCornerBuildable(groundCell.PossiblePrototypes[0].MeshRot, corner, out _))
            {
                Cells[cellIndex.x, cellIndex.y] = new Cell(
                    true,
                    cellPosition,
                    unbuildablePrototypeList,
                    false);
            }
        }
    }

    private bool LoadPrototypeData()
    {
        if (townPrototypeInfo == null)
        {
            Debug.LogError("Please enter prototype reference");
            return false;
        }

        prototypes = townPrototypeInfo.Prototypes;
        return prototypes.Count > 0;
    }

    #endregion

    #region Events

    private void OnBuildingRepaired(Building building)
    {
        this[building.Index] = new Cell(true, this[building.Index].Position, this[building.Index].PossiblePrototypes);
    }

    private void OnBuildingDestroyed(Building building)
    {
        return;
        this[building.Index] = new Cell(false, this[building.Index].Position, prototypes);
        List<int2> cellsToUpdate = new List<int2>();
        for (int i = 0; i < WaveFunctionUtility.NeighbourDirections.Length; i++)
        {
            int2 index = building.Index + new int2(WaveFunctionUtility.NeighbourDirections[i].x, WaveFunctionUtility.NeighbourDirections[i].y);
            if (this[index].Collapsed)
            {
                cellsToUpdate.Add(index);
            }
        }

        allowedKeys = keyData.BuildingKeys;
        MakeBuildable(cellsToUpdate);
        
        Propagate();

        int tries = 1000;
        while (cellsToCollapse.Any(x => !Cells[x.x, x.y].Collapsed) && tries-- > 0)
        {
            Iterate();
        }

        if (tries <= 0)
        {
            RevertQuery();
            return;
        }
        Place();
    }

    #endregion

    #region Query & Place

    public void Place()
    {
        Events.OnBuildingBuilt?.Invoke(querySpawnedBuildings.Values);

        foreach (var item in querySpawnedBuildings)
        {
            item.Value.ToggleIsBuildableVisual(false);
        }
        
        UncollapseAir();

        foreach (var item in querySpawnedBuildings)
        {
            spawnedMeshes.Add(item.Key, item.Value);
        }

        shouldRemoveQueryIndex = false;
        querySpawnedBuildings.Clear();
        queryChangedCells.Clear();
    }

    private void UncollapseAir()
    {
        for (int i = 0; i < queryCollapsedAir.Count; i++)
        {
            int2 index = queryCollapsedAir[i];
            bool fixedit = false;
            for (int g = 0; g < queryChangedCells.Count; g++)
            {
                if (math.all(index != queryChangedCells[g].Item1)) continue;
                
                fixedit = true;
                int2 changedIndex = queryChangedCells[g].Item1;
                if (queryChangedCells[g].Item2.Collapsed)
                {
                    SetCell(changedIndex, queryChangedCells[g].Item2.PossiblePrototypes[0], false);
                }
                else
                {
                    this[changedIndex] = queryChangedCells[g].Item2;
                }
                break;
            }
            
            if (!fixedit)
            {
                this[index] = new Cell(false, this[index].Position, new List<PrototypeData> { emptyPrototype });
            }
        }
        queryCollapsedAir.Clear();
    }

    public void RevertQuery()
    {
        foreach (var item in querySpawnedBuildings)
        {
            item.Value.gameObject.SetActive(false);
        }
        querySpawnedBuildings.Clear();

        for (int i = 0; i < queryChangedCells.Count; i++)
        {
            int2 index = queryChangedCells[i].Item1;
            if (queryChangedCells[i].Item2.Collapsed)
            {
                SetCell(index, queryChangedCells[i].Item2.PossiblePrototypes[0], false);
            }
            else
            {
                Cells[index.x, index.y] = queryChangedCells[i].Item2;
            }
        }

        if (shouldRemoveQueryIndex)
        {
            shouldRemoveQueryIndex = false;
            cellsBuilt[queryIndex.x, queryIndex.y] = false;
        }
        
        queryChangedCells.Clear();
        queryCollapsedAir.Clear();
    }

    public Dictionary<int2, IBuildable> Query(int2 queryIndex, BuildingType buildingType)
    {
        if (querySpawnedBuildings.Count > 0)
        {
            RevertQuery();
        }

        currentBuildingType = buildingType;
        cellsToCollapse = GetCellsToCollapse(queryIndex, buildingType);
        if (cellsToCollapse.Count <= 0) return querySpawnedBuildings;
        
        this.queryIndex = queryIndex;
        if (cellsBuilt[queryIndex.x, queryIndex.y])
        {
            shouldRemoveQueryIndex = false;
        }
        else
        {
            shouldRemoveQueryIndex = true;
            cellsBuilt[queryIndex.x, queryIndex.y] = true;
        }
        
        switch (buildingType)
        {
            case BuildingType.Building:
                allowedKeys = keyData.BuildingKeys;
                MakeBuildable(cellsToCollapse);
                break;
                
            case BuildingType.Path:
                allowedKeys = keyData.PathKeys;
                MakeBuildable(cellsToCollapse);
                break;
        }
        Propagate();

        int tries = 1000;
        while (cellsToCollapse.Any(x => !Cells[x.x, x.y].Collapsed) && tries-- > 0)
        {
            Iterate();
        }

        if (tries <= 0)
        {
            Debug.LogError("Ran out of attempts to collapse");
        }

        return querySpawnedBuildings;
    }

    private void MakeBuildable(List<int2> cellsToCollapse) 
    {
        for (int i = 0; i < cellsToCollapse.Count; i++)
        {
            int2 index = cellsToCollapse[i];
            if (!this[index].Buildable) continue;

            int marchedIndex = GetMarchIndex(index);
            queryChangedCells.Add((index, Cells[index.x, index.y]));
            Cells[index.x, index.y] = new Cell(false, 
                Cells[index.x, index.y].Position, 
                new List<PrototypeData>(townPrototypeInfo.MarchingTable[marchedIndex]));

            ValidDirections(index, out _).ForEach(x => cellStack.Push(x));

            if (spawnedMeshes.TryGetValue(index, out IBuildable buildable))
            {
                buildable.gameObject.SetActive(false);
                spawnedMeshes.Remove(index);
            }
        }
    }

    public List<int2> GetCellsToCollapse(int2 queryIndex, BuildingType type)
    {
        queryIndex += new int2(1, 1);
        return GetCellsToCollapse(this[queryIndex].Position + new Vector3(0.1f, 0, 0.1f), type);
    }
    
    private List<int2> GetCellsToCollapse(Vector3 queryPos, BuildingType type)
    {
        List<int2> toCollapse = GetSurroundingCells(queryPos);

        //cellsToCollapse.ForEach((x) => Debug.Log("Cell: " + x));
        return toCollapse;
    }

    #endregion

    #region Core
    private void Iterate()
    {
        int2 index = GetLowestEntropyIndex();

        PrototypeData chosenPrototype = Collapse(Cells[index.x, index.y]);
        SetCell(index, chosenPrototype);

        Propagate();
    }

    private int2 GetLowestEntropyIndex()
    {
        float lowestEntropy = 10000;
        int2 index = new int2();

        for (int i = 0; i < cellsToCollapse.Count; i++)
        {
            Cell cell = Cells[cellsToCollapse[i].x, cellsToCollapse[i].y];
            if (cell.Collapsed)
            {
                continue;
            }

            float possibleMeshAmount = WaveFunctionUtility.CalculateEntropy(cell);
            
            if (possibleMeshAmount < lowestEntropy)
            {
                lowestEntropy = possibleMeshAmount;
                index = cellsToCollapse[i];
            }
        }

        return index;
    }

    private PrototypeData Collapse(Cell cell)
    {
        if (cell.PossiblePrototypes.Count == 0)
        {
            return emptyPrototype;
        }

        float totalCount = 0;
        for (int i = 0; i < cell.PossiblePrototypes.Count; i++)
        {
            totalCount += cell.PossiblePrototypes[i].Weight;
        }

        float randomIndex = UnityEngine.Random.Range(0, totalCount);
        int index = 0;
        for (int i = 0; i < cell.PossiblePrototypes.Count; i++)
        {
            randomIndex -= cell.PossiblePrototypes[i].Weight;
            if (randomIndex <= 0)
            {
                index = i;
                break;
            }
        }

        return cell.PossiblePrototypes[index];
    }

    private void SetCell(int2 index, PrototypeData chosenPrototype, bool query = true)
    {
        Cells[index.x, index.y] = new Cell(true, Cells[index.x, index.y].Position, new List<PrototypeData> { chosenPrototype });
        cellStack.Push(index);

        IBuildable spawned = GenerateMesh(Cells[index.x, index.y].Position, chosenPrototype);
        if (spawned == null) 
        {
            queryCollapsedAir.Add(index);
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

    private void Propagate()
    {
        while (cellStack.TryPop(out int2 cellIndex))
        {
            Cell changedCell = Cells[cellIndex.x, cellIndex.y];
            List<int2> validDirs = ValidDirections(cellIndex, out List<Direction> directions);

            for (int i = 0; i < validDirs.Count; i++)
            {
                Cell neighbour = Cells[validDirs[i].x, validDirs[i].y];
                Direction dir = directions[i];

                Constrain(changedCell, neighbour, dir, out bool changed);

                if (changed)
                {
                    cellStack.Push(validDirs[i]);
                }
            }
        }
    }

    #endregion

    #region Constain

    private List<int2> ValidDirections(int2 index, out List<Direction> directions) // Exclude should be a flag
    {
        List<int2> valids = new List<int2>();
        directions = new List<Direction>();

        // Right
        if (index.x + 1 < Cells.GetLength(0))
        {
            valids.Add(index + new int2(1, 0));
            directions.Add(Direction.Right);
        }

        // Left
        if (index.x - 1 >= 0)
        {
            valids.Add(index + new int2(-1, 0));
            directions.Add(Direction.Left);
        }
        
        // Forward
        if (index.y + 1 < Cells.GetLength(1))
        {
            valids.Add(index + new int2(0, 1));
            directions.Add(Direction.Forward);
        }

        // Backward
        if (index.y - 1 >= 0)
        {
            valids.Add(index + new int2(0, -1));
            directions.Add(Direction.Backward);
        }

        return valids;
    }

    private void Constrain(Cell changedCell, Cell affectedCell, Direction direction, out bool changed)
    {
        if (affectedCell.Collapsed)
        {
            changed = false;
            return;
        }

        HashSet<short> validKeys = new HashSet<short>();
        for (int i = 0; i < changedCell.PossiblePrototypes.Count; i++)
        {
            validKeys.Add(changedCell.PossiblePrototypes[i].DirectionToKey(direction));
        }

        changed = false;
        var oppositeDirection = WaveFunctionUtility.OppositeDirection(direction);
        for (int i = affectedCell.PossiblePrototypes.Count - 1; i >= 0; i--)
        {
            PrototypeData prot = affectedCell.PossiblePrototypes[i];
            if (!prot.Keys.Any(x => allowedKeys.Contains(x))) // Could be pre-calculated based on prototype
            {
                affectedCell.PossiblePrototypes.RemoveAtSwapBack(i);
                continue;
            }

            bool shouldRemove = !WaveFunctionUtility.CheckValidSocket(prot.DirectionToKey(oppositeDirection), validKeys); 

            if (shouldRemove)
            {
                affectedCell.PossiblePrototypes.RemoveAtSwapBack(i);
                changed = true;
            }
        }

        if (affectedCell.PossiblePrototypes.Count == 0)
        {
            affectedCell.PossiblePrototypes.Add(emptyPrototype);
        }
    }

    #endregion

    #region Utility
    
    private int GetMarchIndex(int2 index)
    {
        int marchedIndex = 0;
        for (int i = 0; i < marchDirections.Count; i++)
        {
            int2 marchIndex = new int2(index.x + marchDirections[i].x, index.y + marchDirections[i].y);
            if (cellsBuilt.IsInBounds(marchIndex) && cellsBuilt[marchIndex.x, marchIndex.y])
            {
                marchedIndex += (int)Mathf.Pow(2, i);
            }
        }
        
        return marchedIndex;
    }
    
    private List<int2> GetAllCells(Vector3 min, Vector3 max)
    {
        List<int2> surrounding = new List<int2>();

        for (float x = min.x; x <= max.x; x += groundGenerator.ChunkWaveFunction.GridScale.x * cellSize)
        {
            for (float y = min.y; y <= max.y; y += groundGenerator.ChunkWaveFunction.GridScale.y)
            {
                for (float z = min.z; z <= max.z; z += groundGenerator.ChunkWaveFunction.GridScale.z * cellSize)
                {
                    int2? index = GetIndex(new Vector3(x, y, z));
                    if (index.HasValue)
                        surrounding.Add(index.Value);
                }
            }
        }

        return surrounding;
    }

    private List<int2> GetSurroundingCells(Vector3 queryPosition)
    {
        List<int2> surrounding = new List<int2>();

        for (int x = -1; x <= 1; x += 2)
        {
            for (int z = -1; z <= 1; z += 2)
            {
                int2? index = GetIndex(queryPosition + cellSize * x * Vector3.right + Vector3.forward * z * cellSize);
                if (index.HasValue)
                {
                    surrounding.Add(index.Value);
                }
            }
        }

        return surrounding;
    }

    public int2? GetIndex(Vector3 pos)
    {
        int2 index = new int2(Math.GetMultiple(pos.x, groundGenerator.ChunkWaveFunction.GridScale.x * cellSize), Math.GetMultiple(pos.z, groundGenerator.ChunkWaveFunction.GridScale.z * cellSize));
        if (Cells.IsInBounds(index.x, index.y))
        {
            return index;
        }
        
        return null;
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

        Vector3 scale = Vector3.one * cellSize;
        building.Setup(prototypeData, scale);

        if (animate) buildingAnimator.Animate(building);

        return building;
    }

    #endregion

    #region API

    public Vector3 GetPos(int2 index)
    {
        return Cells[index.x, index.y].Position;
    }
    
    #endregion
    
    #region Debug

    private void OnDrawGizmosSelected()
    {
        if (!EditorApplication.isPlaying || Cells == null || Cells.Length == 0)
        {
            return;
        }

        for (int y = 0; y < Cells.GetLength(1); y++)
        {
            for (int x = 0; x < Cells.GetLength(0); x++)
            {
                Vector3 pos = Cells[x, y].Position + new Vector3(groundGenerator.ChunkWaveFunction.GridScale.x * cellSize / 2.0f, 0, groundGenerator.ChunkWaveFunction.GridScale.z * cellSize / 2.0f);
                Gizmos.color = !cellsBuilt[x, y] ? Color.white : Color.magenta;
                Gizmos.DrawWireCube(pos, new Vector3(groundGenerator.ChunkWaveFunction.GridScale.x * cellSize, groundGenerator.ChunkWaveFunction.GridScale.y, groundGenerator.ChunkWaveFunction.GridScale.z * cellSize) * 0.75f);
            }
        }
    }

    #endregion
}
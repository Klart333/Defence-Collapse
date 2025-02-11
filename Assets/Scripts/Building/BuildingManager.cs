using System.Collections.Generic;
using Debug = UnityEngine.Debug;
using Sirenix.OdinInspector;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Buildings;
using System;
using Buildings.District;
using Unity.Collections;
using UnityEngine.Serialization;

public class BuildingManager : Singleton<BuildingManager> 
{
    public event Action<Vector3Int> OnCastlePlaced;
    public event Action OnLoaded;

    [Title("Cells")]
    [SerializeField]
    private float cellSize = 1f; // Needs to be an exponent of 2, probably

    [Title("Prototypes")]
    [SerializeField]
    private PrototypeInfoCreator townPrototypeInfo;

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

    private bool[,,] cellsBuilt;

    public Cell this[Vector3Int index]
    {
        get => Cells[index.x, index.y, index.z];
        private set => Cells[index.x, index.y, index.z] = value;
    }

    private readonly List<Vector2Int> marchDirections = new List<Vector2Int>
    {
        new Vector2Int(-1, -1),
        new Vector2Int(0, -1),
        new Vector2Int(0, 0), 
        new Vector2Int(-1, 0),
    };
    
    private readonly Dictionary<Vector3Int, IBuildable> querySpawnedBuildings = new Dictionary<Vector3Int, IBuildable>();
    private readonly Dictionary<Vector3Int, IBuildable> spawnedMeshes = new Dictionary<Vector3Int, IBuildable>();
    private readonly List<(Vector3Int, Cell)> queryChangedCells = new List<(Vector3Int, Cell)>();
    private readonly List<Vector3Int> queryCollapsedAir = new List<Vector3Int>();
    private readonly Stack<Vector3Int> cellStack = new Stack<Vector3Int>();
    
    private List<PrototypeData> unbuildablePrototypeList;
    private List<PrototypeData> prototypes = new List<PrototypeData>();
    private List<Vector3Int> cellsToCollapse = new List<Vector3Int>();
    private HashSet<string> allowedKeys;

    private GroundGenerator groundGenerator;
    private BuildingAnimator buildingAnimator;
    private PrototypeData emptyPrototype;
    private PrototypeData unbuildablePrototype;
    private Vector3Int queryIndex;

    private BuildingType currentBuildingType;
    
    private bool shouldRemoveQueryIndex;
        
    public int TopBuildableLayer { get; private set; }
    public Cell[,,] Cells { get; private set; }
    public float CellSize => cellSize;

    private void OnEnable()
    {
        groundGenerator = FindFirstObjectByType<GroundGenerator>();
        buildingAnimator = GetComponent<BuildingAnimator>();

        groundGenerator.OnMapGenerated += Load;
        Events.OnBuildingDestroyed += OnBuildingDestroyed;
        Events.OnBuildingRepaired += OnBuildingRepaired;
    }
    
    private void OnDisable()
    {
        groundGenerator.OnMapGenerated -= Load;
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

    private void Load()
    {
        if (!LoadPrototypeData())
        {
            Debug.LogError("No prototype data found");
            return;
        }

        LoadCells();

        OnLoaded?.Invoke();
    }

    private void LoadCells()
    {
        Cells = new Cell[Mathf.RoundToInt(groundGenerator.WaveFunction.GridSize.x / cellSize), groundGenerator.WaveFunction.GridSize.y + 1, Mathf.RoundToInt(groundGenerator.WaveFunction.GridSize.z / cellSize)];
        cellsBuilt = new bool[Mathf.RoundToInt(groundGenerator.WaveFunction.GridSize.x / cellSize), groundGenerator.WaveFunction.GridSize.y + 1, Mathf.RoundToInt(groundGenerator.WaveFunction.GridSize.z / cellSize)];
        emptyPrototype = new PrototypeData(new MeshWithRotation(null, 0), "-1s", "-1s", "-1s", "-1s", "-1s", "-1s", 1, Array.Empty<int>());
        unbuildablePrototype = new PrototypeData(new MeshWithRotation(null, 0), "-1s", "-1s", "-1s", "-1s", "-1s", "-1s", 0, Array.Empty<int>());
        unbuildablePrototypeList = new List<PrototypeData> { unbuildablePrototype };

        for (int z = 0; z < Cells.GetLength(2); z++)
        {
            for (int y = 0; y < Cells.GetLength(1); y++)
            {
                for (int x = 0; x < Cells.GetLength(0); x++)
                {
                    Vector3 pos = new Vector3(x * cellSize * groundGenerator.WaveFunction.GridScale.x, y * groundGenerator.WaveFunction.GridScale.y, z * cellSize * groundGenerator.WaveFunction.GridScale.z) - new Vector3(cellSize * groundGenerator.WaveFunction.GridScale.x, 0, cellSize * groundGenerator.WaveFunction.GridScale.z) / 2.0f;
                    Cells[x, y, z] = new Cell(false, pos + transform.position, new List<PrototypeData> { emptyPrototype }, y > 0);
                }
            }
        }

        for (int z = 0; z < Cells.GetLength(2); z++)
        {
            for (int y = 0; y < Cells.GetLength(1); y++)
            {
                for (int x = 0; x < Cells.GetLength(0); x++)
                {
                    Vector3Int cellIndex = new Vector3Int(x, y, z);
                    Vector3Int gridIndex = new Vector3Int(Mathf.FloorToInt(x * cellSize), y, Mathf.FloorToInt(z * cellSize));
                    SetCell(cellIndex, gridIndex); 
                }
            }
        }
    }

    private void SetCell(Vector3Int cellIndex, Vector3Int gridIndex)
    {
        Vector3 cellPosition = Cells[cellIndex.x, cellIndex.y, cellIndex.z].Position;
        if (gridIndex.y == Cells.GetLength(1) - 1) // Just put air at the top
        {
            Cells[cellIndex.x, cellIndex.y, cellIndex.z] = new Cell(true, cellPosition, unbuildablePrototypeList, false);
            return;
        }

        Cell groundCell = groundGenerator.WaveFunction.GetCellAtIndexInverse(gridIndex);
        /*if (groundCell.PossiblePrototypes[0].MeshRot.Mesh is not null)
        {
            Debug.DrawLine(cellPosition, groundCell.Position, Color.red, 100, false);
        }*/
        
        Vector2Int corner = new Vector2Int((int)Mathf.Sign(groundCell.Position.x - cellPosition.x), (int)Mathf.Sign(groundCell.Position.z - cellPosition.z));

        if (!cellBuildableCornerData.IsBuildable(groundCell.PossiblePrototypes[0].MeshRot, corner, out _))
        {
            Cells[cellIndex.x, cellIndex.y, cellIndex.z] = new Cell(
                true,
                cellPosition,
                unbuildablePrototypeList,
                false);
        }
        else if (cellIndex.y > TopBuildableLayer)
        {
            TopBuildableLayer = cellIndex.y;
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
        this[building.Index] = new Cell(true, this[building.Index].Position, this[building.Index].PossiblePrototypes, true);
    }

    private void OnBuildingDestroyed(Building building)
    {
        this[building.Index] = new Cell(true, this[building.Index].Position, this[building.Index].PossiblePrototypes, false);
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
            Vector3Int index = queryCollapsedAir[i];
            bool fixedit = false;
            for (int g = 0; g < queryChangedCells.Count; g++)
            {
                if (index != queryChangedCells[g].Item1) continue;
                
                fixedit = true;
                Vector3Int changedIndex = queryChangedCells[g].Item1;
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
            Vector3Int index = queryChangedCells[i].Item1;
            if (queryChangedCells[i].Item2.Collapsed)
            {
                SetCell(index, queryChangedCells[i].Item2.PossiblePrototypes[0], false);
            }
            else
            {
                Cells[index.x, index.y, index.z] = queryChangedCells[i].Item2;
            }
        }

        if (shouldRemoveQueryIndex)
        {
            shouldRemoveQueryIndex = false;
            cellsBuilt[queryIndex.x, queryIndex.y, queryIndex.z] = false;
        }
        
        queryChangedCells.Clear();
        queryCollapsedAir.Clear();
    }

    public Dictionary<Vector3Int, IBuildable> Query(Vector3Int queryIndex, BuildingType buildingType)
    {
        if (querySpawnedBuildings.Count > 0)
        {
            RevertQuery();
        }

        currentBuildingType = buildingType;
        cellsToCollapse = GetCellsToCollapse(queryIndex, buildingType);
        if (cellsToCollapse.Count <= 0) return querySpawnedBuildings;
        
        this.queryIndex = queryIndex;
        if (cellsBuilt[queryIndex.x, queryIndex.y, queryIndex.z])
        {
            shouldRemoveQueryIndex = false;
        }
        else
        {
            shouldRemoveQueryIndex = true;
            cellsBuilt[queryIndex.x, queryIndex.y, queryIndex.z] = true;
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
        while (cellsToCollapse.Any(x => !Cells[x.x, x.y, x.z].Collapsed) && tries-- > 0)
        {
            Iterate();
        }

        if (tries <= 0)
        {
            Debug.LogError("Ran out of attempts to collapse");
        }

        return querySpawnedBuildings;
    }

    private void MakeBuildable(List<Vector3Int> cellsToCollapse) 
    {
        for (int i = 0; i < cellsToCollapse.Count; i++)
        {
            Vector3Int index = cellsToCollapse[i];
            if (!this[index].Buildable) continue;

            int marchedIndex = GetMarchIndex(index);
            queryChangedCells.Add((index, Cells[index.x, index.y, index.z]));
            Cells[index.x, index.y, index.z] = new Cell(false, 
                Cells[index.x, index.y, index.z].Position, 
                new List<PrototypeData>(townPrototypeInfo.MarchingTable[marchedIndex]));

            ValidDirections(index, out _).ForEach(x => cellStack.Push(x));

            if (spawnedMeshes.TryGetValue(index, out IBuildable buildable))
            {
                buildable.gameObject.SetActive(false);
                spawnedMeshes.Remove(index);
            }
        }
    }

    public List<Vector3Int> GetCellsToCollapse(Vector3Int queryIndex, BuildingType type)
    {
        queryIndex += new Vector3Int(1, 0, 1);
        return GetCellsToCollapse(this[queryIndex].Position + new Vector3(0.1f, 0, 0.1f), type);
    }
    
    private List<Vector3Int> GetCellsToCollapse(Vector3 queryPos, BuildingType type)
    {
        List<Vector3Int> toCollapse = GetSurroundingCells(queryPos);

        //cellsToCollapse.ForEach((x) => Debug.Log("Cell: " + x));
        return toCollapse;
    }

    #endregion

    #region Core
    private void Iterate()
    {
        Vector3Int index = GetLowestEntropyIndex();

        PrototypeData chosenPrototype = Collapse(Cells[index.x, index.y, index.z]);
        SetCell(index, chosenPrototype);

        Propagate();
    }

    private Vector3Int GetLowestEntropyIndex()
    {
        float lowestEntropy = 10000;
        Vector3Int index = new Vector3Int();

        for (int i = 0; i < cellsToCollapse.Count; i++)
        {
            Cell cell = Cells[cellsToCollapse[i].x, cellsToCollapse[i].y, cellsToCollapse[i].z];
            if (cell.Collapsed)
            {
                continue;
            }

            float possibleMeshAmount = 0;
            float totalWeight = 0;
            for (int g = 0; g < cell.PossiblePrototypes.Count; g++)
            {
                totalWeight += cell.PossiblePrototypes[g].Weight;
            }

            float averageWeight = totalWeight / cell.PossiblePrototypes.Count;
            for (int g = 0; g < cell.PossiblePrototypes.Count; g++)
            {
                float distFromAverage = 1.0f - (cell.PossiblePrototypes[g].Weight / averageWeight);
                if (distFromAverage < 1.0f) distFromAverage *= distFromAverage; // Because of using the percentage as a distance, smaller weights weigh more, so this is is to try to correct that.

                possibleMeshAmount += Mathf.Lerp(1, 0, Mathf.Abs(distFromAverage));
            }

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

    private void SetCell(Vector3Int index, PrototypeData chosenPrototype, bool query = true)
    {
        Cells[index.x, index.y, index.z] = new Cell(true, Cells[index.x, index.y, index.z].Position, new List<PrototypeData> { chosenPrototype });
        cellStack.Push(index);

        IBuildable spawned = GenerateMesh(Cells[index.x, index.y, index.z].Position, chosenPrototype);
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
        while (cellStack.TryPop(out Vector3Int cellIndex))
        {
            Cell changedCell = Cells[cellIndex.x, cellIndex.y, cellIndex.z];
            List<Vector3Int> validDirs = ValidDirections(cellIndex, out List<Direction> directions);

            for (int i = 0; i < validDirs.Count; i++)
            {
                Cell neighbour = Cells[validDirs[i].x, validDirs[i].y, validDirs[i].z];
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

    private List<Vector3Int> ValidDirections(Vector3Int index, out List<Direction> directions) // Exclude should be a flag
    {
        List<Vector3Int> valids = new List<Vector3Int>();
        directions = new List<Direction>();

        // Right
        if (index.x + 1 < Cells.GetLength(0))
        {
            valids.Add(index + Vector3Int.right);
            directions.Add(Direction.Right);
        }

        // Left
        if (index.x - 1 >= 0)
        {
            valids.Add(index + Vector3Int.left);
            directions.Add(Direction.Left);
        }

        // Up
        //if (index.y + 1 < Cells.GetLength(1))
        //{
        //    valids.Add(index + Vector3Int.up);
        //    directions.Add(Direction.Up);
        //}

        // Down
        //if (index.y - 1 >= 0)
        //{
        //    valids.Add(index + Vector3Int.down);
        //    directions.Add(Direction.Down);
        //}

        // Forward
        if (index.z + 1 < Cells.GetLength(2))
        {
            valids.Add(index + Vector3Int.forward);
            directions.Add(Direction.Forward);
        }

        // Backward
        if (index.z - 1 >= 0)
        {
            valids.Add(index + Vector3Int.back);
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

        HashSet<string> validKeys = new HashSet<string>();
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
    
    private int GetMarchIndex(Vector3Int index)
    {
        int marchedIndex = 0;
        for (int i = 0; i < marchDirections.Count; i++)
        {
            Vector3Int marchIndex = new Vector3Int(index.x + marchDirections[i].x, index.y, index.z + marchDirections[i].y);
            if (cellsBuilt[marchIndex.x, marchIndex.y, marchIndex.z])
            {
                marchedIndex += (int)Mathf.Pow(2, i);
            }
        }
        
        return marchedIndex;
    }
    
    private List<Vector3Int> GetAllCells(Vector3 min, Vector3 max)
    {
        List<Vector3Int> surrounding = new List<Vector3Int>();

        for (float x = min.x; x <= max.x; x += groundGenerator.WaveFunction.GridScale.x * cellSize)
        {
            for (float y = min.y; y <= max.y; y += groundGenerator.WaveFunction.GridScale.y)
            {
                for (float z = min.z; z <= max.z; z += groundGenerator.WaveFunction.GridScale.z * cellSize)
                {
                    Vector3Int? index = GetIndex(new Vector3(x, y, z));
                    if (index.HasValue)
                        surrounding.Add(index.Value);
                }
            }
        }

        return surrounding;
    }

    private List<Vector3Int> GetSurroundingCells(Vector3 queryPosition)
    {
        List<Vector3Int> surrounding = new List<Vector3Int>();

        for (int x = -1; x <= 1; x += 2)
        {
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3Int? index = GetIndex(queryPosition + cellSize * x * Vector3.right + Vector3.forward * z * cellSize);
                if (index.HasValue)
                {
                    surrounding.Add(index.Value);
                }
            }
        }

        return surrounding;
    }

    public Vector3Int? GetIndex(Vector3 pos)
    {
        Vector3Int index = new Vector3Int(Math.GetMultiple(pos.x, groundGenerator.WaveFunction.GridScale.x * cellSize), Math.GetMultiple(pos.y, groundGenerator.WaveFunction.GridScale.y), Math.GetMultiple(pos.z, groundGenerator.WaveFunction.GridScale.z * cellSize));
        if (Cells.IsInBounds(index.x, index.y, index.z))
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

    public Vector3 GetPos(Vector3Int index)
    {
        return Cells[index.x, index.y, index.z].Position;
    }
    
    #endregion
    
    #region Debug

    private void OnDrawGizmosSelected()
    {
        if (!EditorApplication.isPlaying || Cells == null || Cells.Length == 0)
        {
            return;
        }

        for (int z = 0; z < Cells.GetLength(2); z++)
        {
            for (int y = 0; y < 2; y++)
            {
                for (int x = 0; x < Cells.GetLength(0); x++)
                {
                    Vector3 pos = Cells[x, y, z].Position + new Vector3(groundGenerator.WaveFunction.GridScale.x * cellSize / 2.0f, 0, groundGenerator.WaveFunction.GridScale.z * cellSize / 2.0f);
                    Gizmos.color = !cellsBuilt[x, y, z] ? Color.white : Color.magenta;
                    Gizmos.DrawWireCube(pos, new Vector3(groundGenerator.WaveFunction.GridScale.x * cellSize, groundGenerator.WaveFunction.GridScale.y, groundGenerator.WaveFunction.GridScale.z * cellSize) * 0.75f);
                }
            }
        }
    }

    #endregion
}

public static class ArrayHelper
{
    public static bool IsInBounds<T>(this T[,,] array, int x, int y, int z)
    {
        if (x < 0 || x >= array.GetLength(0))
            return false;
        if (y < 0 || y >= array.GetLength(1))
            return false;
        if (z < 0 || z >= array.GetLength(2))
            return false;

        return true;
    }

    public static bool IsInBounds<T>(this T[,,] array, Vector3Int index)
    {
        return array.IsInBounds(index.x, index.y, index.z);
    }

    public static bool IsInBounds<T>(this T[,] array, int x, int y)
    {
        if (x < 0 || x >= array.GetLength(0))
            return false;
        if (y < 0 || y >= array.GetLength(1))
            return false;

        return true;
    }

    public static bool IsInBounds<T>(this T[,] array, Vector2Int index)
    {
        return array.IsInBounds(index.x, index.y);
    }
}

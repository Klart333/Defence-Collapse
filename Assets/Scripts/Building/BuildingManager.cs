using System.Collections.Generic;
using Debug = UnityEngine.Debug;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using Sirenix.Utilities;
using System.Linq;
using UnityEngine;
using Buildings;
using System;
using UnityEditor;
using UnityEngine.UIElements;

public class BuildingManager : Singleton<BuildingManager> 
{
    public event Action<Vector3Int> OnCastlePlaced;

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

    [Title("Castle")]
    [SerializeField]
    private int CastleIndex = 20;

    [Title("Debug")]
    [SerializeField]
    private PooledMonoBehaviour unableToPlacePrefab;

    [SerializeField]
    private bool DebugPropagate;

    [SerializeField, ShowIf(nameof(DebugPropagate)), Range(1, 1000)]
    private int Speed;

    public const float CellSize = 0.5f; // Needs to be an exponent of 2, probably

    private Cell[,,] cells;

    public Cell this[Vector3Int index]
    {
        get { return cells[index.x, index.y, index.z]; }
        set { cells[index.x, index.y, index.z] = value; }
    }

    private Dictionary<Vector3Int, IBuildable> spawnedMeshes = new Dictionary<Vector3Int, IBuildable>();
    private Dictionary<Vector3Int, IBuildable> querySpawnedBuildings = new Dictionary<Vector3Int, IBuildable>();
    private List<(Vector3Int, Cell)> queryChangedCells = new List<(Vector3Int, Cell)>();
    private List<Vector3Int> queryCollapsedAir = new List<Vector3Int>();

    private List<PrototypeData> prototypes = new List<PrototypeData>();
    private List<Vector3Int> cellsToCollapse = new List<Vector3Int>();
    private Stack<Vector3Int> cellStack = new Stack<Vector3Int>();

    private WaveFunction waveFunction;
    private BuildingAnimator buildingAnimator;

    private PrototypeData emptyPrototype;
    private PrototypeData unbuildablePrototype;

    private BuildingType currentBuildingType;

    private HashSet<string> allowedKeys;

    private void OnEnable()
    {
        waveFunction = FindFirstObjectByType<WaveFunction>();
        buildingAnimator = GetComponent<BuildingAnimator>();

        waveFunction.OnMapGenerated += Load;
        Events.OnBuildingDestroyed += OnBuildingDestroyed;
        Events.OnBuildingRepaired += OnBuildingRepaired;
    }
    private void OnDisable()
    {
        waveFunction.OnMapGenerated -= Load;
        Events.OnBuildingDestroyed += OnBuildingDestroyed;
        Events.OnBuildingRepaired += OnBuildingRepaired;
    }

    [Button]
    public void ShowCollapsedTiles()
    {
        foreach (var item in cells)
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
        return;
    }

    private void LoadCells()
    {
        cells = new Cell[Mathf.RoundToInt(waveFunction.GridSize.x / CellSize), waveFunction.GridSize.y + 1, Mathf.RoundToInt(waveFunction.GridSize.z / CellSize)];
        emptyPrototype = new PrototypeData(new MeshWithRotation(null, 0), "-1s", "-1s", "-1s", "-1s", "-1s", "-1s", 1, new int[0]);
        unbuildablePrototype = new PrototypeData(new MeshWithRotation(null, 0), "-1s", "-1s", "-1s", "-1s", "-1s", "-1s", 0, new int[0]);

        for (int z = 0; z < cells.GetLength(2); z++)
        {
            for (int y = 0; y < cells.GetLength(1); y++)
            {
                for (int x = 0; x < cells.GetLength(0); x++)
                {
                    Vector3 pos = new Vector3(x * CellSize * waveFunction.GridScale.x, y * waveFunction.GridScale.y, z * CellSize * waveFunction.GridScale.z);
                    cells[x, y, z] = new Cell(false, pos + transform.position, new List<PrototypeData>() { emptyPrototype });
                }
            }
        }

        for (int z = 0; z < cells.GetLength(2); z++)
        {
            for (int y = 0; y < cells.GetLength(1); y++)
            {
                for (int x = 0; x < cells.GetLength(0); x++)
                {
                    Vector3Int cellIndex = new Vector3Int(x, y, z);
                    Vector3Int gridIndex = new Vector3Int(Mathf.FloorToInt(x * CellSize), y, Mathf.FloorToInt(z * CellSize));
                    SetCell(cellIndex, gridIndex); 
                }
            }
        }
    }

    private void SetCell(Vector3Int cellIndex, Vector3Int gridIndex)
    {
        if (gridIndex.y == cells.GetLength(1) - 1) // Just put air at the top
        {
            cells[cellIndex.x, cellIndex.y, cellIndex.z] = new Cell(true, cells[cellIndex.x, cellIndex.y, gridIndex.z].Position, new List<PrototypeData>() { unbuildablePrototype }, false);
            return;
        }

        Cell groundCell = waveFunction.GetCellAtIndexInverse(gridIndex);
        if (groundCell.PossiblePrototypes[0].MeshRot.Mesh == null) // It's air
        {
            cells[cellIndex.x, cellIndex.y, cellIndex.z] = new Cell(
                true, 
                cells[cellIndex.x, cellIndex.y, cellIndex.z].Position, 
                new List<PrototypeData>() { unbuildablePrototype }, 
                false);
        }
        else if (groundCell.PossiblePrototypes[0].MeshRot.Mesh.name == "Ground_Portal") // yay hard coded names :))))
        {
            //int index = 0 + ((groundCell.PossiblePrototypes[0].MeshRot.Rot + 1) % 4);
            //SetCell(new Vector3Int(x, y, z), pathPrototypeInfo.Prototypes[index], false);
            //SetCell(new Vector3Int(x, y - 1, z), groundPathPrototype, false);
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

        if (currentBuildingType == BuildingType.Castle)
        {
            OnCastlePlaced?.Invoke(cellsToCollapse[4]);
        }

        foreach (var item in querySpawnedBuildings)
        {
            item.Value.ToggleIsBuildableVisual(false);
        }
        
        UncollapseAir();

        spawnedMeshes.AddRange(querySpawnedBuildings);
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
                if (index == queryChangedCells[g].Item1)
                {
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
            }
            
            if (!fixedit)
            {
                this[index] = new Cell(false, this[index].Position, new List<PrototypeData>() { emptyPrototype });
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
                cells[index.x, index.y, index.z] = queryChangedCells[i].Item2;
            }
        }
        queryChangedCells.Clear();
        queryCollapsedAir.Clear();
    }

    public Dictionary<Vector3Int, IBuildable> Query(Vector3 queryPosition, BuildingType buildingType)
    {
        if (querySpawnedBuildings.Count > 0)
        {
            RevertQuery();
        }

        currentBuildingType = buildingType;
        cellsToCollapse = GetCellsToCollapse(queryPosition, buildingType);

        switch (buildingType)
        {
            case BuildingType.Castle:
                allowedKeys = keyData.BuildingKeys;
                MakeBuildable(cellsToCollapse);
                Propagate();
                if (cellsToCollapse.Count < 9 || this[cellsToCollapse[4]].PossiblePrototypes.Count == 1)
                {
                    return querySpawnedBuildings;
                }
                SetCell(cellsToCollapse[4], prototypes[CastleIndex]);
                break;

            case BuildingType.Building:
                allowedKeys = keyData.BuildingKeys;
                MakeBuildable(cellsToCollapse);
                break;
                
            case BuildingType.Path:
                allowedKeys = keyData.PathKeys;
                MakeBuildable(cellsToCollapse);
                break;
            default:
                break;
        }
        Propagate();

        while (!cellsToCollapse.All(x => cells[x.x, x.y, x.z].Collapsed))
        {
            Iterate();
        }

        return querySpawnedBuildings;
    }

    private void MakeBuildable(List<Vector3Int> cellsToCollapse) 
    {
        for (int i = 0; i < cellsToCollapse.Count; i++)
        {
            Vector3Int index = cellsToCollapse[i];
            if (!this[index].Buildable)
            {
                continue;
            }

            queryChangedCells.Add((index, cells[index.x, index.y, index.z]));

            cells[index.x, index.y, index.z] = new Cell(false, cells[index.x, index.y, index.z].Position, new List<PrototypeData>(prototypes));
            ValidDirections(index, out _).ForEach(x => cellStack.Push(x));

            if (spawnedMeshes.TryGetValue(index, out IBuildable buildable))
            {
                buildable.gameObject.SetActive(false);
                spawnedMeshes.Remove(index);
            }
        }
    }

    public List<Vector3Int> GetCellsToCollapse(Vector3 queryPos, BuildingType type)
    {
        List<Vector3Int> cellsToCollapse = new List<Vector3Int>();

        switch (type)
        {
            case BuildingType.Castle:
                Vector3 minPos = new(queryPos.x - CellSize * 2, queryPos.y, queryPos.z - CellSize * 2);
                Vector3 maxPos = new(queryPos.x + CellSize * 2, queryPos.y, queryPos.z + CellSize * 2);
                cellsToCollapse = GetAllCells(minPos, maxPos);
                break;
            case BuildingType.Building:
                cellsToCollapse = GetSurroundingCells(queryPos);

                break;
            case BuildingType.Path:
                cellsToCollapse = GetSurroundingCells(queryPos);
                cellsToCollapse.AddRange(GetSurroundingCells(queryPos + Vector3.up * waveFunction.GridScale.y));
                break;
        }
        //cellsToCollapse.ForEach((x) => Debug.Log("Cell: " + x));

        return cellsToCollapse;
    }

    #endregion

    #region Core
    public void Iterate()
    {
        Vector3Int index = GetLowestEntropyIndex();

        PrototypeData chosenPrototype = Collapse(cells[index.x, index.y, index.z]);
        SetCell(index, chosenPrototype);

        Propagate();
    }

    private Vector3Int GetLowestEntropyIndex()
    {
        float lowestEntropy = 10000;
        Vector3Int index = new Vector3Int();

        for (int i = 0; i < cellsToCollapse.Count; i++)
        {
            Cell cell = cells[cellsToCollapse[i].x, cellsToCollapse[i].y, cellsToCollapse[i].z];
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
        cells[index.x, index.y, index.z] = new Cell(true, cells[index.x, index.y, index.z].Position, new List<PrototypeData>() { chosenPrototype });
        cellStack.Push(index);

        IBuildable spawned = GenerateMesh(cells[index.x, index.y, index.z].Position, chosenPrototype);
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
        if (spawnedMeshes.ContainsKey(index))
        {
            spawnedMeshes[index] = spawned;
        }
        else
        {
            spawnedMeshes.Add(index, spawned);
        }
    }

    public void Propagate()
    {
        while (cellStack.TryPop(out Vector3Int cellIndex))
        {
            Cell changedCell = cells[cellIndex.x, cellIndex.y, cellIndex.z];

            List<Vector3Int> validDirs = ValidDirections(cellIndex, out List<Direction> directions);

            for (int i = 0; i < validDirs.Count; i++)
            {
                Cell neighbour = cells[validDirs[i].x, validDirs[i].y, validDirs[i].z];
                Direction dir = directions[i];

                Constrain(changedCell, neighbour, dir, out bool changed);

                if (changed)
                {
                    cellStack.Push(validDirs[i]);
                }
            }

            /*if (DebugPropagate)
            {
                DisplayPossiblePrototypes();
                await Task.Delay(Speed);
            }*/
        }
    }

    #endregion

    #region Constain

    private List<Vector3Int> ValidDirections(Vector3Int index, out List<Direction> directions, Direction exlcude = Direction.None) // Exclude should be a flag
    {
        List<Vector3Int> valids = new List<Vector3Int>();
        directions = new List<Direction>();

        // Right
        if (index.x + 1 < cells.GetLength(0) && Direction.Right != exlcude)
        {
            valids.Add(index + Vector3Int.right);
            directions.Add(Direction.Right);
        }

        // Left
        if (index.x - 1 >= 0 && Direction.Left != exlcude)
        {
            valids.Add(index + Vector3Int.left);
            directions.Add(Direction.Left);
        }

        // Up
        if (index.y + 1 < cells.GetLength(1) && Direction.Up != exlcude)
        {
            valids.Add(index + Vector3Int.up);
            directions.Add(Direction.Up);
        }

        // Down
        if (index.y - 1 >= 0 && Direction.Down != exlcude)
        {
            valids.Add(index + Vector3Int.down);
            directions.Add(Direction.Down);
        }

        // Forward
        if (index.z + 1 < cells.GetLength(2) && Direction.Forward != exlcude)
        {
            valids.Add(index + Vector3Int.forward);
            directions.Add(Direction.Forward);
        }

        // Backward
        if (index.z - 1 >= 0 && Direction.Backward != exlcude)
        {
            valids.Add(index + Vector3Int.back);
            directions.Add(Direction.Backward);
        }


        return valids;
    }

    private List<PrototypeData> Constrain(Cell changedCell, Cell affectedCell, Direction direction, out bool changed)
    {
        if (affectedCell.Collapsed)
        {
            changed = false;
            return null;
        }

        List<string> validKeys = new List<string>();
        for (int i = 0; i < changedCell.PossiblePrototypes.Count; i++)
        {
            switch (direction)
            {
                case Direction.Right:
                    validKeys.Add(changedCell.PossiblePrototypes[i].PosX);
                    break;

                case Direction.Left:
                    validKeys.Add(changedCell.PossiblePrototypes[i].NegX);
                    break;

                case Direction.Up:
                    validKeys.Add(changedCell.PossiblePrototypes[i].PosY);
                    break;

                case Direction.Down:
                    validKeys.Add(changedCell.PossiblePrototypes[i].NegY);
                    break;

                case Direction.Forward:
                    validKeys.Add(changedCell.PossiblePrototypes[i].PosZ);
                    break;

                case Direction.Backward:
                    validKeys.Add(changedCell.PossiblePrototypes[i].NegZ);
                    break;

            }
        }

        changed = false;
        for (int i = 0; i < affectedCell.PossiblePrototypes.Count; i++)
        {
            PrototypeData prot = affectedCell.PossiblePrototypes[i];
            if (!prot.Keys.Any(x => allowedKeys.Contains(x))) // Could be pre-calculated based on prototype
            {
                affectedCell.PossiblePrototypes.RemoveAt(i--);
                continue;
            }

            bool shouldRemove = false;
            switch (direction)
            {
                case Direction.Right:
                    shouldRemove = !CheckValidSocket(prot.NegX, validKeys);

                    break;
                case Direction.Left:
                    shouldRemove = !CheckValidSocket(prot.PosX, validKeys);

                    break;
                case Direction.Up:
                    shouldRemove = !CheckValidSocket(prot.NegY, validKeys);

                    break;
                case Direction.Down:
                    shouldRemove = !CheckValidSocket(prot.PosY, validKeys);

                    break;
                case Direction.Forward:
                    shouldRemove = !CheckValidSocket(prot.NegZ, validKeys);

                    break;
                case Direction.Backward:
                    shouldRemove = !CheckValidSocket(prot.PosZ, validKeys);

                    break;
            }

            if (shouldRemove)
            {
                affectedCell.PossiblePrototypes.RemoveAt(i--);
                changed = true;
            }
        }

        if (affectedCell.PossiblePrototypes.Count == 0)
        {
            affectedCell.PossiblePrototypes.Add(emptyPrototype);
        }
        return affectedCell.PossiblePrototypes;
    }

    private bool CheckValidSocket(string key, List<string> validKeys)
    {
        if (key.Contains('v')) // Ex. v0_0
        {
            return validKeys.Contains(key);
        }
        else if (key.Contains('s')) // Ex. 0s
        {
            return validKeys.Contains(key);
        }
        else if (key.Contains('f')) // Ex. 0f
        {
            return validKeys.Contains(key.Replace("f", ""));
        }
        else // Ex. 0
        {
            string keyf = key + 'f';
            return validKeys.Contains(keyf);
        }
    }

    #endregion

    #region Utility

    private List<Vector3Int> GetAllCells(Vector3 min, Vector3 max)
    {
        List<Vector3Int> surrounding = new List<Vector3Int>();

        for (float x = min.x; x <= max.x; x += waveFunction.GridScale.x * CellSize)
        {
            for (float y = min.y; y <= max.y; y += waveFunction.GridScale.y)
            {
                for (float z = min.z; z <= max.z; z += waveFunction.GridScale.z * CellSize)
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
                Vector3Int? index = GetIndex(queryPosition + CellSize * x * Vector3.right + Vector3.forward * z * CellSize);
                if (index.HasValue)
                {
                    surrounding.Add(index.Value);
                }
            }
        }

        return surrounding;
    }

    public HashSet<Vector3Int> GetPerimeterCells(List<Vector3Int> cells)
    {
        HashSet<Vector3Int> cellSet = new HashSet<Vector3Int>(cells);

        foreach (var cell in cells)
        {
            foreach (Vector3Int dir in PathFinding.XYDirections)
            {
                Vector3Int neighbor = cell + dir;
                if (!cells.Contains(neighbor))
                {
                    cellSet.Add(neighbor);
                }
            }
        }

        return cellSet;
    }

    public Vector3Int? GetIndex(Vector3 pos)
    {
        Vector3Int index = new Vector3Int(Math.GetMultiple(pos.x, waveFunction.GridScale.x * CellSize), Math.GetMultiple(pos.y, waveFunction.GridScale.y), Math.GetMultiple(pos.z, waveFunction.GridScale.z * CellSize));
        if (cells.IsInBounds(index.x, index.y, index.z))
        {
            return index;
        }
        
        return null;
    }

    public Vector3 GetPos(Vector3Int index)
    {
        return cells[index.x, index.y, index.z].Position;
    }

    private IBuildable GenerateMesh(Vector3 position, PrototypeData prototypeData, bool animate = true)
    {
        if (prototypeData.MeshRot.Mesh == null)
        {
            return null;
        }

        IBuildable building;
        if (prototypeData.MeshRot.Mesh.name.Contains("Path")) // Not my best work
        {
            building = pathPrefab.GetAtPosAndRot<Path>(position, Quaternion.Euler(0, 90 * prototypeData.MeshRot.Rot, 0));
        }
        else
        {
            building = buildingPrefab.GetAtPosAndRot<Building>(position, Quaternion.Euler(0, 90 * prototypeData.MeshRot.Rot, 0));
        }

        Vector3 scale = new Vector3(CellSize, waveFunction.GridScale.y / 2.0f, CellSize);
        building.Setup(prototypeData, scale);

        if (animate) buildingAnimator.Animate(building);

        return building;
    }

    #endregion

    #region Debug

    private void OnDrawGizmosSelected()
    {
        if (!EditorApplication.isPlaying)
        {
            return;
        }

        for (int z = 0; z < cells.GetLength(2); z++)
        {
            for (int y = 0; y < cells.GetLength(1); y++)
            {
                for (int x = 0; x < cells.GetLength(0); x++)
                {
                    Vector3 pos = cells[x, y, z].Position;
                    Gizmos.color = cells[x, y, z].Buildable ? Color.white : Color.red;
                    Gizmos.DrawWireCube(pos, new Vector3(waveFunction.GridScale.x * CellSize, waveFunction.GridScale.y, waveFunction.GridScale.z * CellSize) * 0.4f);
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


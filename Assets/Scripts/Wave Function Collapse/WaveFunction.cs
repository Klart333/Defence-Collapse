using Debug = UnityEngine.Debug;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using System;

public class WaveFunction : MonoBehaviour
{
    public event Action OnMapGenerated;

    [Header("Grid Points")]
    [SerializeField]
    private int gridSizeX = 5;

    [SerializeField]
    private int gridSizeY = 1;

    [SerializeField]
    private int gridSizeZ = 5;

    [Header("Size")]
    [SerializeField]
    private Vector3 gridSize;

    [Header("Mesh")]
    [SerializeField]
    private MaterialData materialData;

    [Header("Prototypes")]
    [SerializeField]
    private PrototypeInfoCreator groundPrototypeInfo;

    [Header("Speed Limits")]
    [SerializeField]
    private float maxMillisecondsPerFrame = 4;

    [SerializeField]
    private bool shouldCombine = true;

    [SerializeField]
    private int chillTimeMs = 0;

    private List<GameObject> spawnedPossibilites = new List<GameObject>();
    private List<GameObject> spawnedMeshes = new List<GameObject>();

    private List<PrototypeData> prototypes = new List<PrototypeData>();
    private List<PrototypeData> bottomPrototypes = new List<PrototypeData>();
    private List<Cell> cells = new List<Cell>();

    private Stack<int> cellStack = new Stack<int>();

    private PrototypeData emptyPrototype;

    public GameObject SpawnedCastle { get; private set; }
    public int Speed { get; set; }
    public Vector3Int GridSize => new Vector3Int(gridSizeX, gridSizeY, gridSizeZ);
    public Vector3 GridScale => gridSize;
    private bool AllCollapsed
    {
        get
        {
            return !cells.Any(cell => !cell.Collapsed);
        }
    }

    private void Start()
    {
        Run();
    }

    public async void Run()
    {
        if (!Load())
        {
            return;
        }

        //await PredeterminePath();
        await BottomBuildUp();

        Stopwatch watch = Stopwatch.StartNew();
        while (!AllCollapsed)
        {
            Iterate(); // Does not need to await
           
            if (watch.ElapsedMilliseconds > maxMillisecondsPerFrame)
            {
                await UniTask.NextFrame();
                if (chillTimeMs > 0)
                {
                    await UniTask.Delay(chillTimeMs);
                }

                watch.Restart();
            }
        }

        if (shouldCombine)
        {
            CombineMeshes();
        }

        OnMapGenerated?.Invoke();
    }

    private bool Load()
    {
        Clear();

        if (!LoadPrototypeData())
        {
            Debug.LogError("No prototype data found");
            return false;
        }

        LoadCells();
        return true;
    }

    private async UniTask BottomBuildUp()
    {
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int z = 0; z < gridSizeZ; z++)
            {
                if ((x == 0 || x == gridSizeX - 1) && (z == 0 || z == gridSizeZ - 1))
                {
                    await PlaceGround(x, z);
                    continue;
                }

                if ((x != 0 && x != gridSizeX - 1) && (z != 0 && z != gridSizeZ - 1))
                {
                    continue;
                }

                if (UnityEngine.Random.value < 0.8f)
                {
                    continue;
                }

                await PlaceGround(x, z);
            }
        }

        async UniTask PlaceGround(int x, int z)
        {
            int index = GetIndex(x, 0, z);
            if (cells[index].PossiblePrototypes.Contains(prototypes[2 * 4]))
            {
                SetCell(index, prototypes[2 * 4]);

                Propagate();

                await UniTask.Yield();
            }
        }
    }

    public void Iterate()
    {
        int index = GetLowestEntropyIndex();

        PrototypeData chosenPrototype = Collapse(cells[index]);
        SetCell(index, chosenPrototype);

        Propagate();
    }

    private int GetLowestEntropyIndex()
    {
        float lowestEntropy = 10000;
        int index = 0;

        for (int i = 0; i < cells.Count; i++)
        {
            if (cells[i].Collapsed)
            {
                continue;
            }

            float possibleMeshAmount = 0;

            possibleMeshAmount += (GetCords(i).y) * 100; // Add the Y level
            if (possibleMeshAmount > lowestEntropy)
            {
                continue;
            }

            float totalWeight = 0;
            for (int g = 0; g < cells[i].PossiblePrototypes.Count; g++)
            {
                totalWeight += cells[i].PossiblePrototypes[g].Weight;
            }

            float averageWeight = totalWeight / cells[i].PossiblePrototypes.Count;
            for (int g = 0; g < cells[i].PossiblePrototypes.Count; g++)
            {
                float distFromAverage = 1.0f - (cells[i].PossiblePrototypes[g].Weight / averageWeight);
                if (distFromAverage < 1.0f) distFromAverage *= distFromAverage; // Because of using the percentage as a distance, smaller weights weigh more, so this is is to try to correct that.

                possibleMeshAmount += Mathf.Lerp(1, 0, Mathf.Abs(distFromAverage));
            }

            if (possibleMeshAmount < lowestEntropy)
            {
                lowestEntropy = possibleMeshAmount;
                index = i;
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

        int index = 0;
        float randomIndex = UnityEngine.Random.Range(0, totalCount);
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

    private void SetCell(int index, PrototypeData chosenPrototype)
    {
        cells[index] = new Cell(true, cells[index].Position, new List<PrototypeData>() { chosenPrototype });
        cellStack.Push(index);

        GameObject spawned = GenerateMesh(cells[index].Position, chosenPrototype);
        if (spawned != null)
        {
            spawnedMeshes.Add(spawned);
        }
    }

    public void Propagate()
    {
        while (cellStack.TryPop(out int cellIndex))
        {
            if (cellIndex >= cells.Count)
            {
                continue;
            }

            Cell changedCell = cells[cellIndex];

            List<int> validDirs = ValidDirections(cellIndex, out List<Direction> directions);

            for (int i = 0; i < validDirs.Count; i++)
            {
                Cell neighbour = cells[validDirs[i]];
                Direction dir = directions[i];

                var constrainedPrototypes = Constrain(changedCell, neighbour, validDirs[i], dir, out bool changed);

                if (changed)
                {
                    cells[validDirs[i]] = new Cell(neighbour.Collapsed, neighbour.Position, constrainedPrototypes);
                    cellStack.Push(validDirs[i]);
                } 
            }
        }
    }

    private List<PrototypeData> Constrain(Cell changedCell, Cell affectedCell, int index, Direction direction, out bool changed)
    {
        if (affectedCell.Collapsed)
        {
            changed = false;
            return new List<PrototypeData>(affectedCell.PossiblePrototypes);
        }

        // Check downward
        // Rule is above flat tile is only air
        {
            int directIndex = index;
            List<string> upKeys = new List<string>();
            bool onlyAir = true;
            while (IsDirectionValid(directIndex, Direction.Down, out directIndex) && onlyAir)
            {
                onlyAir = true;
                upKeys.Clear();
                for (int i = 0; i < cells[directIndex].PossiblePrototypes.Count; i++)
                {
                    upKeys.Add(cells[directIndex].PossiblePrototypes[i].PosY);

                    if (onlyAir)
                    {
                        if (!string.Equals(cells[directIndex].PossiblePrototypes[i].NegY, "-1s") || !string.Equals(cells[directIndex].PossiblePrototypes[i].PosX, "-1s") || !string.Equals(cells[directIndex].PossiblePrototypes[i].NegX, "-1s") || !string.Equals(cells[directIndex].PossiblePrototypes[i].NegZ, "-1s") || !string.Equals(cells[directIndex].PossiblePrototypes[i].PosZ, "-1s"))
                        {
                            onlyAir = false;
                        }
                    }
                }
            }

            // If we exited because we found a tile with not only air and not because the direction became invalid
            if (!onlyAir && upKeys.All(x => string.Equals(x, "-1s")))
            {
                SetCell(index, emptyPrototype);
                changed = false;
                return new List<PrototypeData>() { emptyPrototype };
            }
        }

        // Check Directly Above
        // Rule is Below flat tile is only air
        {
            if (IsDirectionValid(index, Direction.Up, out int aboveIndex))
            {
                if (cells[aboveIndex].Collapsed)
                {
                    if (cells[aboveIndex].PossiblePrototypes[0].NegY == "-1s")
                    {
                        bool onlyAir = true;
                        if (!string.Equals(cells[aboveIndex].PossiblePrototypes[0].PosY, "-1s") || !string.Equals(cells[aboveIndex].PossiblePrototypes[0].PosX, "-1s") || !string.Equals(cells[aboveIndex].PossiblePrototypes[0].NegX, "-1s") || !string.Equals(cells[aboveIndex].PossiblePrototypes[0].NegZ, "-1s") || !string.Equals(cells[aboveIndex].PossiblePrototypes[0].PosZ, "-1s"))
                        {
                            onlyAir = false;
                        }

                        if (!onlyAir)
                        {
                            SetCell(index, emptyPrototype);
                            changed = false;
                            return new List<PrototypeData>() { emptyPrototype };
                        }
                    }
                }
            }
        }

        List<string> validKeys = new List<string>();
        for (int i = 0; i < changedCell.PossiblePrototypes.Count; i++)
        {
            // Right
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

        int removed = 0;
        for (int i = 0; i < affectedCell.PossiblePrototypes.Count; i++)
        {
            bool shouldRemove = false;
            switch (direction)
            {
                case Direction.Right:
                    shouldRemove = !CheckValidSocket(affectedCell.PossiblePrototypes[i].NegX, validKeys);

                    break;
                case Direction.Left:
                    shouldRemove = !CheckValidSocket(affectedCell.PossiblePrototypes[i].PosX, validKeys);

                    break;
                case Direction.Up:
                    shouldRemove = !CheckValidSocket(affectedCell.PossiblePrototypes[i].NegY, validKeys);

                    break;
                case Direction.Down:
                    shouldRemove = !CheckValidSocket(affectedCell.PossiblePrototypes[i].PosY, validKeys);

                    break;
                case Direction.Forward:
                    shouldRemove = !CheckValidSocket(affectedCell.PossiblePrototypes[i].NegZ, validKeys);

                    break;
                case Direction.Backward:
                    shouldRemove = !CheckValidSocket(affectedCell.PossiblePrototypes[i].PosZ, validKeys);

                    break;
            }

            if (shouldRemove)
            {
                affectedCell.PossiblePrototypes.RemoveAt(i--);
                removed++;
            }
        }

        changed = removed > 0;
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

    private bool IsDirectionValid(int index, Direction direction, out int directIndex)
    {
        switch (direction)
        {
            case Direction.Right:
                if (index % gridSizeX + 1 < gridSizeX)
                {
                    directIndex = index + 1;
                    return true;
                }
                break;
            case Direction.Left:
                if (index % gridSizeX - 1 >= 0)
                {
                    directIndex = index - 1;
                    return true;
                }
                break;
            case Direction.Up:
                int thing = 1 + Mathf.FloorToInt((float)index / (float)(gridSizeX * gridSizeY));
                if ((index + gridSizeX) < (gridSizeX * gridSizeY) * thing)
                {
                    directIndex = index + gridSizeX;
                    return true;
                }
                break;
            case Direction.Down:
                int thing1 = 1 + Mathf.FloorToInt((float)index / (float)(gridSizeX * gridSizeY));
                if ((index - gridSizeX) >= (gridSizeX * gridSizeY) * (thing1 - 1))
                {
                    directIndex = index - gridSizeX;
                    return true;
                }
                break;
            case Direction.Forward:
                if (index + (gridSizeX * gridSizeY) < cells.Count)
                {
                    directIndex = index + (gridSizeX * gridSizeY);
                    return true;
                }
                break;
            case Direction.Backward:
                if (index - (gridSizeX * gridSizeY) >= 0)
                {
                    directIndex = index - (gridSizeX * gridSizeY);
                    return true;
                }
                break;
            default:
                break;
        }

        directIndex = -1;
        return false;
    }

    private List<int> ValidDirections(int index, out List<Direction> directions, bool all = true)
    {
        List<int> valids = new List<int>();
        directions = new List<Direction>();

        if (all)
        {
            // Up
            int thing = 1 + Mathf.FloorToInt((float)index / (float)(gridSizeX * gridSizeY));
            if ((index + gridSizeX) < (gridSizeX * gridSizeY) * thing)
            {
                valids.Add(index + gridSizeX);
                directions.Add(Direction.Up);
            }

            // Down
            if ((index - gridSizeX) >= (gridSizeX * gridSizeY) * (thing - 1))
            {
                valids.Add(index - gridSizeX);
                directions.Add(Direction.Down);
            }
        }

        // Right
        if (index % gridSizeX + 1 < gridSizeX)
        {
            valids.Add(index + 1);
            directions.Add(Direction.Right);
        }

        // Left
        if (index % gridSizeX - 1 >= 0)
        {
            valids.Add(index - 1);
            directions.Add(Direction.Left);
        }

        // Forward
        if (index + (gridSizeX * gridSizeY) < cells.Count)
        {
            valids.Add(index + (gridSizeX * gridSizeY));
            directions.Add(Direction.Forward);
        }

        // Backward
        if (index - (gridSizeX * gridSizeY) >= 0)
        {
            valids.Add(index - (gridSizeX * gridSizeY));
            directions.Add(Direction.Backward);
        }

        return valids;
    }

    private Vector3Int GetCords(int index)
    {
        int x = Mathf.FloorToInt(index / (gridSizeZ * gridSizeY));
        int y = Mathf.FloorToInt(index / gridSizeZ) % gridSizeY;
        int z = index % gridSizeZ;

        return new Vector3Int(x, y, z);
    }

    private int GetIndex(int x, int y, int z)
    {
        return (x * gridSizeY * gridSizeZ) + (y * gridSizeZ) + z;
    }

    private bool LoadPrototypeData()
    {
        if (groundPrototypeInfo == null)
        {
            Debug.LogError("Please enter prototype reference");
            return false;
        }

        prototypes = groundPrototypeInfo.Prototypes;

        bottomPrototypes.Clear();
        for (int i = 0; i < prototypes.Count; i++)
        {
            if (groundPrototypeInfo.NotAllowedForBottom.Contains(i))
            {
                continue;
            }

            bottomPrototypes.Add(prototypes[i]);
        }

        return prototypes.Count > 0;
    }

    private void LoadCells()
    {
        emptyPrototype = new PrototypeData(new MeshWithRotation(null, 0), "-1s", "-1s", "-1s", "-1s", "-1s", "-1s", 20, new int[0]);

        for (int z = 0; z < gridSizeZ; z++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                for (int x = 0; x < gridSizeX; x++)
                {
                    Vector3 pos = new Vector3(x * gridSize.x, y * gridSize.y, z * gridSize.z);

                    List<PrototypeData> prots;

                    if (y == 0)
                    {
                        prots = new List<PrototypeData>(bottomPrototypes);
                    }
                    else
                    {
                        prots = new List<PrototypeData>(prototypes);

                        if (x == 0 || x == gridSizeX - 1 || z == 0 || z == gridSizeZ - 1)
                        {
                            for (int i = 0; i < groundPrototypeInfo.NotAllowedForSides.Count; i++)
                            {
                                prots.RemoveAt(groundPrototypeInfo.NotAllowedForSides[i]);
                            }
                        }
                    }

                    cells.Add(new Cell(false, pos + transform.position, prots));
                }
            }
        }
    }

    private GameObject GenerateMesh(Vector3 position, PrototypeData prototypeData, float scale = 1)
    {
        if (prototypeData.MeshRot.Mesh == null)
        {
            return null;
        }

        GameObject gm = new GameObject();
        gm.AddComponent<MeshFilter>().mesh = prototypeData.MeshRot.Mesh;
        gm.AddComponent<MeshRenderer>().SetMaterials(materialData.GetMaterials(prototypeData.MaterialIndexes));

        gm.transform.position = position;
        gm.transform.rotation = Quaternion.Euler(0, 90 * prototypeData.MeshRot.Rot, 0);
        gm.transform.SetParent(transform, true);
        
        gm.transform.localScale = GridScale / 2 * scale;

        return gm;
    }

    private void CombineMeshes()
    {
        GetComponent<MeshCombiner>().CombineMeshes();
    }

    public void Clear()
    {
        for (int i = 0; i < spawnedMeshes.Count; i++)
        {
            DestroyImmediate(spawnedMeshes[i]);
        }

        spawnedMeshes.Clear();
        cells.Clear();
    }

    public void DisplayPossiblePrototypes()
    {
        HidePossiblePrototypes();

        for (int i = 0; i < cells.Count; i++)
        {
            if (cells[i].Collapsed)
            {
                continue;
            }

            float scale = 1.0f / cells[i].PossiblePrototypes.Count;
            int removed = 0;
            for (int g = 0; g < cells[i].PossiblePrototypes.Count; g++)
            {
                if (cells[i].PossiblePrototypes[g].MeshRot.Mesh == null)
                {
                    removed++;
                    continue;
                }

                float offset = (1.0f / cells[i].PossiblePrototypes.Count) * (((float)(g + 1 - removed) * 2) - cells[i].PossiblePrototypes.Count);
                Vector3 pos = cells[i].Position + Vector3.right * offset;

                spawnedPossibilites.Add(GenerateMesh(pos, cells[i].PossiblePrototypes[g], scale));
            }
        }
    }

    public void HidePossiblePrototypes()
    {
        for (int i = 0; i < spawnedPossibilites.Count; i++)
        {
            DestroyImmediate(spawnedPossibilites[i]);
        }

        spawnedPossibilites.Clear();
    }

    #region API

    public Cell GetCellAtIndexInverse(Vector3Int index)
    {
        int dex = (index.z * gridSizeY * gridSizeZ) + (index.y * gridSizeZ) + index.x;
        return cells[dex];
    }

    public Cell GetCellAtIndexInverse(int x, int y, int z)
    {
        int dex = (z * gridSizeY * gridSizeZ) + (y * gridSizeZ) + x;
        return cells[dex];
    }

    public Cell GetCellAtIndex(Vector3Int index)
    {
        return GetCellAtIndex(index.x, index.y, index.z);
    }

    public Cell GetCellAtIndex(int x, int y, int z)
    {
        return cells[GetIndex(x, y, z)];
    }

    public void SetGridSize(Vector3Int size)
    {
        gridSizeX = size.x;
        gridSizeY = size.y;
        gridSizeZ = size.z;
    }

    #endregion

    #region Debug

    private void OnDrawGizmosSelected()
    {
        if (!UnityEditor.EditorApplication.isPlaying)
        {
            return;
        }

        foreach (var cell in cells)
        {
            Vector3 pos = cell.Position;
            Gizmos.color = cell.Buildable ? Color.white : Color.red;
            Gizmos.DrawWireCube(pos, new Vector3(GridScale.x, GridScale.y, GridScale.z) * 0.75f);
        }
    }

    #endregion
}
[System.Serializable]
public struct MeshWithRotation
{
    public Mesh Mesh;
    public int Rot;

    public MeshWithRotation(Mesh mesh, int rot)
    {
        Mesh = mesh;
        Rot = rot;
    }
}

[System.Serializable]
public struct Cell
{
    public bool Collapsed;
    public bool Buildable;

    public Vector3 Position;

    public List<PrototypeData> PossiblePrototypes;

    public Cell(bool collapsed, Vector3 position, List<PrototypeData> possiblePrototypes, bool buildable = true)
    {
        this.Collapsed = collapsed;
        this.Position = position;
        PossiblePrototypes = possiblePrototypes;
        Buildable = buildable;
    }
}

public struct Node
{
    public bool Walkable;
    public Vector3 Position;

    public Node(bool walkable, Vector3 position)
    {
        Walkable = walkable;
        Position = position;
    }
}

public enum Direction
{
    Right, 
    Left,
    Up,
    Down,
    Forward,
    Backward,
    None
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

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
    private List<Material> mats;

    [Header("Rules")]
    [SerializeField]
    private int[] notAllowedForBottom;

    [Header("Path")]
    [SerializeField]
    private int turnWeight = 20;

    [SerializeField]
    private GameObject castle;

    [Header("BottomBuildup")]
    [SerializeField]
    private int[] allowedBuildUp;

    [Header("Debug")]
    public bool Manual;

    private List<GameObject> spawnedPossibilites = new List<GameObject>();
    private List<GameObject> spawnedMeshes = new List<GameObject>();

    private List<PrototypeData> prototypes = new List<PrototypeData>();
    private List<PrototypeData> bottomPrototypes = new List<PrototypeData>();
    private List<Cell> cells = new List<Cell>();
    private List<Cell> cellPath = new List<Cell>();

    private Stack<int> cellStack = new Stack<int>();

    private Node[,] map;

    private PrototypeData emptyPrototype;

    public GameObject SpawnedCastle { get; private set; }
    public bool Auto { get; set; }
    public int Speed { get; set; }
    public Vector3 GridSize => new Vector3(gridSizeX, gridSizeY, gridSizeZ);
    private bool AllCollapsed
    {
        get
        {
            return !cells.Any(cell => !cell.Collapsed);
        }
    }
    private int startCord => gridSizeZ * (gridSizeY + 1) + gridSizeZ / 2;
    private float minLength => gridSizeZ + gridSizeZ / 3.0f;

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

        if (Manual && !Auto)
        {
            return;
        }

        await PredeterminePath();
        await BottomBuildUp();

        int i = 0;
        while (!AllCollapsed)
        {
            await Iterate(); // Does not need to await
           
            if (i++ % 3 == 0) await Task.Yield();
        }

        CombineMeshes();
        GetMap();

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

    private async Task PredeterminePath() 
    {
        int index = startCord;
        cellPath.Add(cells[index]);

        do
        {
            SetCell(index, prototypes[2 * 4]);
            await Propagate();

            await Task.Yield();
        } while (Move(ref index));

        if (Vector3.Distance(cells[index].Position, cells[startCord].Position) < minLength)
        {
            if (!Load())
            {
                return;
            } 

            await PredeterminePath();
            return;
        }

        SpawnedCastle = Instantiate(castle, cells[index].Position, Quaternion.identity);

        bool Move(ref int index)
        {
            List<int> neighbours = ValidDirections(index, out List<Direction> dirs, false);
            if (cellPath.Count > 20 && neighbours.Count < 4)
            {
                return false;
            }

            for (int i = 0; i < neighbours.Count; i++)
            {
                if (cells[neighbours[i]].Collapsed)
                {
                    neighbours.RemoveAt(i--);
                    continue;
                }

                List<int> neighboursNeighbours = ValidDirections(neighbours[i], out List<Direction> dirs1, false);

                bool checksOut = true;
                int amount = 0;
                for (int g = 0; g < neighboursNeighbours.Count; g++)
                {
                    if (cells[neighboursNeighbours[g]].Collapsed)
                    {
                        amount++;
                        if (amount >= 2)
                        {
                            checksOut = false;
                            break;
                        }
                        
                    }
                }

                if (!checksOut)
                {
                    neighbours.RemoveAt(i--);
                    continue;
                }
            }

            if (neighbours.Count <= 0)
            {
                return false;
            }

            if (cellPath.Count >= 2)
            {
                Vector3 pastDir = (cellPath[cellPath.Count - 1].Position - cellPath[cellPath.Count - 2].Position).normalized;
                int[] weights = new int[neighbours.Count];
                for (int i = 0; i < weights.Length; i++)
                {
                    if (Vector3.Dot((cells[neighbours[i]].Position - cells[index].Position).normalized, pastDir) > 0.8f)
                    {
                        weights[i] = 100;
                        continue;
                    }
                    weights[i] = turnWeight;
                }

                int totalCount = 0;
                for (int i = 0; i < neighbours.Count; i++)
                {
                    totalCount += weights[i];
                }

                int chosenIndex = 0;
                int randomIndex = UnityEngine.Random.Range(0, totalCount);
                for (int i = 0; i < neighbours.Count; i++)
                {
                    randomIndex -= weights[i];
                    if (randomIndex <= 0)
                    {
                        chosenIndex = i;
                        break;
                    }
                }

                index = neighbours[chosenIndex];

                cellPath.Add(cells[index]);

                return true;
            }           

            int randIndex = UnityEngine.Random.Range(0, neighbours.Count);
            index = neighbours[randIndex];

            cellPath.Add(cells[index]);

            return true;
        }
    }

    private async Task BottomBuildUp()
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

        async Task PlaceGround(int x, int z)
        {
            int index = GetIndex(x, 0, z);
            if (cells[index].PossiblePrototypes.Contains(prototypes[2 * 4]))
            {
                SetCell(index, prototypes[2 * 4]);

                await Propagate();
                await Task.Yield();
            }
        }
    }

    public List<Cell> GetEnemyPath()
    {
        return cellPath;
    }

    public Node[,] GetMap()
    {
        if (map != null)
        {
            return map;
        }

        map = new Node[gridSizeX * 3, gridSizeZ * 3];
        var thiccMap = new Node[gridSizeX * 3, gridSizeZ * 3];

        Collider[] hits = new Collider[2];

        for (int i = 0; i < cells.Count; i++)
        {
            Vector3Int index = GetCords(i);
            if (index.y != 1)
            {
                continue;
            }

            for (int x = 0; x < 3; x++)
            {
                for (int z = 0; z < 3; z++)
                {
                    Vector3 pos = cells[i].Position + new Vector3((gridSize.x / 3.0f) * (z - 1), 0, (gridSize.z / 3.0f) * (x - 1));
                    bool walkable = false;
                    bool thiccWalkable = false;
                    if (Physics.OverlapSphereNonAlloc(pos, 0.2f, hits) == 1)
                    {
                        if (Physics.OverlapSphereNonAlloc(pos + Vector3.up * 0.2f, 0.1f, hits) == 0)
                        {
                            walkable = true;
                        }

                        if (Physics.OverlapSphereNonAlloc(pos + Vector3.up * 1.1f, 1f, hits) == 0)
                        {
                            thiccWalkable = true;
                        }
                    }

                    map[index.x * 3 + x, index.z * 3 + z] = new Node(walkable, pos);
                    thiccMap[index.x * 3 + x, index.z * 3 + z] = new Node(thiccWalkable, pos);
                }
            }
        }

        EnemyPathFinding.Map = map;
        EnemyPathFinding.ThiccMap = thiccMap;
        EnemyPathFinding.FindPath(cellPath[0].Position, cellPath[cellPath.Count - 1].Position, EnemyPathFinding.ThiccMap);
        return map;
    }

    public async Task Iterate()
    {
        int index = GetLowestEntropyIndex();

        PrototypeData chosenPrototype = Collapse(cells[index]);
        SetCell(index, chosenPrototype);

        await Propagate();
    }

    private int GetLowestEntropyIndex()
    {
        float lowestEntropy = 1000;
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

                possibleMeshAmount += Mathf.Lerp(0, 1, Mathf.Abs(distFromAverage));
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

        int totalCount = 0;
        for (int i = 0; i < cell.PossiblePrototypes.Count; i++)
        {
            totalCount += cell.PossiblePrototypes[i].Weight;
        }

        int index = 0;
        int randomIndex = UnityEngine.Random.Range(0, totalCount);
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

        spawnedMeshes.Add(GenerateMesh(cells[index].Position, chosenPrototype));
    }

    public async Task Propagate()
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

                    if (Manual && Auto)
                    {
                        DisplayPossiblePrototypes();
                        await Task.Delay(Speed);
                    }
                } 
            }

            if (Manual)
            {
                DisplayPossiblePrototypes();
                if (Auto)
                {
                    await Task.Delay(Speed);
                }
                else
                {
                    return;
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
            while (IsDirectionValid(directIndex, Direction.Below, out directIndex) && onlyAir)
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
            if (IsDirectionValid(index, Direction.Above, out int aboveIndex))
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
                    
                case Direction.Above:
                    validKeys.Add(changedCell.PossiblePrototypes[i].PosY);
                    break;
                    
                case Direction.Below:
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
                case Direction.Above:
                    shouldRemove = !CheckValidSocket(affectedCell.PossiblePrototypes[i].NegY, validKeys);

                    break;
                case Direction.Below:
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
            case Direction.Above:
                int thing = 1 + Mathf.FloorToInt((float)index / (float)(gridSizeX * gridSizeY));
                if ((index + gridSizeX) < (gridSizeX * gridSizeY) * thing)
                {
                    directIndex = index + gridSizeX;
                    return true;
                }
                break;
            case Direction.Below:
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
                directions.Add(Direction.Above);
            }

            // Down
            if ((index - gridSizeX) >= (gridSizeX * gridSizeY) * (thing - 1))
            {
                valids.Add(index - gridSizeX);
                directions.Add(Direction.Below);
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
        PrototypeInfoCreator prototypeInfoCreator = FindObjectOfType<PrototypeInfoCreator>();
        if (prototypeInfoCreator == null)
        {
            return false;
        }

        prototypes = prototypeInfoCreator.Prototypes;

        bottomPrototypes.Clear();
        for (int i = 0; i < prototypes.Count; i++)
        {
            int meshIndex = Mathf.FloorToInt((float)i / 4.0f);
            if (notAllowedForBottom.Contains(meshIndex))
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

                    if (y == 0)
                    {
                        cells.Add(new Cell(false, pos + transform.position, new List<PrototypeData>(bottomPrototypes)));
                    }
                    else
                    {
                        cells.Add(new Cell(false, pos + transform.position, new List<PrototypeData>(prototypes)));
                    }
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
        gm.AddComponent<MeshRenderer>().materials = mats.Where((x) => prototypeData.MaterialIndexes.Contains(mats.IndexOf(x))).ToArray();

        gm.transform.position = position;
        gm.transform.rotation = Quaternion.Euler(0, 90 * prototypeData.MeshRot.Rot, 0);
        gm.transform.SetParent(transform, true);

        gm.transform.localScale *= scale;

        return gm;
    }

    private void CombineMeshes()
    {
        GetComponent<MeshCombiner>().CombineMeshes();
    }

    public void Clear()
    {
        DestroyImmediate(SpawnedCastle);
        for (int i = 0; i < spawnedMeshes.Count; i++)
        {
            DestroyImmediate(spawnedMeshes[i]);
        }

        cellPath.Clear();
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

    private void OnDrawGizmos()
    {
        /*if (EnemyPathFinding.Map == null)
            return;
        for (int i = 0; i < EnemyPathFinding.Map.GetLength(0); i++)
        {
            for (int g = 0; g < EnemyPathFinding.Map.GetLength(1); g++)
            {
                Handles.Label(EnemyPathFinding.Map[i, g].Position, $"{i}, {g}");

            }
        }

        
        for (int i = 0; i < cells.Count; i++)
        {
            Gizmos.color = Color.black;
            if (cellStack.Count > 0 && i == cellStack.Peek())
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(cells[i].Position + Vector3.up * 0.02f, gridSize);
            }
            else
            {
                Gizmos.color = Color.black;
                Gizmos.DrawWireCube(cells[i].Position, gridSize);
            }
        }*/
    }
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

    public Vector3 Position;

    public List<PrototypeData> PossiblePrototypes;

    public Cell(bool collapsed, Vector3 position, List<PrototypeData> possiblePrototypes)
    {
        this.Collapsed = collapsed;
        this.Position = position;
        PossiblePrototypes = possiblePrototypes;
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
    Above,
    Below,
    Forward,
    Backward,
    None
}
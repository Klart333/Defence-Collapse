using System.Collections.Generic;
using Debug = UnityEngine.Debug;
using System.Linq;
using UnityEngine;
using System;
using Sirenix.OdinInspector;
using Unity.Collections;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

[System.Serializable]
public class WaveFunction
{
    [Title("Grid Size")]
    [SerializeField]
    private int gridSizeX = 5;

    [SerializeField]
    private int gridSizeY = 1;

    [SerializeField]
    private int gridSizeZ = 5;
    
    [SerializeField]
    private Vector3 gridScale;

    [Title("Mesh")]
    [SerializeField]
    private MaterialData materialData;

    [Title("Prototypes")]
    [SerializeField]
    private PrototypeInfoCreator prototypeInfo;
    
    private readonly List<PrototypeData> bottomPrototypes = new List<PrototypeData>();
    private readonly List<GameObject> spawnedPossibilites = new List<GameObject>();
    private readonly List<GameObject> spawnedMeshes = new List<GameObject>();
    private List<PrototypeData> prototypes = new List<PrototypeData>();
    private readonly Stack<int> cellStack = new Stack<int>();
    private readonly List<Cell> cells = new List<Cell>();

    private Transform parentTransform;
    private PrototypeData emptyPrototype;

    public Vector3Int GridSize => new Vector3Int(gridSizeX, gridSizeY, gridSizeZ);
    public bool AllCollapsed => cells.All(cell => cell.Collapsed);
    public List<Cell> Cells => cells;
    public Vector3 GridScale => gridScale;
    
    public Vector3 OriginPosition { get; set; }
    public List<PrototypeData> Prototypes => prototypes;

    public bool Load()
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
    
    public void Iterate()
    {
        int index = GetLowestEntropyIndex();

        PrototypeData chosenPrototype = Collapse(cells[index]);
        SetCell(index, chosenPrototype);

        Propagate();
    }

    public int GetLowestEntropyIndex()
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
            possibleMeshAmount += GetCords(i).y * 100; // Add the Y level
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
                if (distFromAverage < 1.0f) distFromAverage *= distFromAverage; // Because of using the percentage as a distance, smaller weights weigh more, so this is to try to correct that.

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

    public PrototypeData Collapse(Cell cell)
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

    public void SetCell(int index, PrototypeData chosenPrototype)
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

                List<PrototypeData> constrainedPrototypes = Constrain(changedCell, neighbour, validDirs[i], dir, out bool changed);

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
                upKeys.Clear();
                for (int i = 0; i < cells[directIndex].PossiblePrototypes.Count; i++)
                {
                    upKeys.Add(cells[directIndex].PossiblePrototypes[i].PosY);

                    if (onlyAir && (!string.Equals(cells[directIndex].PossiblePrototypes[i].NegY, "-1s") || !string.Equals(cells[directIndex].PossiblePrototypes[i].PosX, "-1s") || !string.Equals(cells[directIndex].PossiblePrototypes[i].NegX, "-1s") || !string.Equals(cells[directIndex].PossiblePrototypes[i].NegZ, "-1s") || !string.Equals(cells[directIndex].PossiblePrototypes[i].PosZ, "-1s")))
                    {
                        onlyAir = false;
                    }
                }
            }

            // If we exited because we found a tile with not only air and not because the direction became invalid
            if (!onlyAir && upKeys.All(x => x[0] == '-')) // key == -1s (only -1s starts with -)
            {
                SetCell(index, emptyPrototype);
                changed = false;
                return new List<PrototypeData> { emptyPrototype };
            }
        }

        // Check Directly Above
        // Rule is Below flat tile is only air
        {
            if (IsDirectionValid(index, Direction.Up, out int aboveIndex))
            {
                if (cells[aboveIndex].Collapsed)
                {
                    if (cells[aboveIndex].PossiblePrototypes[0].NegY[0] == '-')
                    {
                        bool onlyAir = !(cells[aboveIndex].PossiblePrototypes[0].PosY[0] != '-' || cells[aboveIndex].PossiblePrototypes[0].PosX[0] != '-' || cells[aboveIndex].PossiblePrototypes[0].NegX[0] != '-' || cells[aboveIndex].PossiblePrototypes[0].NegZ[0] != '-' || cells[aboveIndex].PossiblePrototypes[0].PosZ[0] != '-');

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
            validKeys.Add(direction switch 
            {
                Direction.Right => changedCell.PossiblePrototypes[i].PosX,
                Direction.Left => changedCell.PossiblePrototypes[i].NegX,
                Direction.Up => changedCell.PossiblePrototypes[i].PosY,
                Direction.Down => changedCell.PossiblePrototypes[i].NegY,
                Direction.Forward => changedCell.PossiblePrototypes[i].PosZ,
                Direction.Backward => changedCell.PossiblePrototypes[i].NegZ,
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
            });
        }

        changed = false;
        for (int i = affectedCell.PossiblePrototypes.Count - 1; i >= 0; i--)
        {
            bool shouldRemove = direction switch
            {
                Direction.Right => !CheckValidSocket(affectedCell.PossiblePrototypes[i].NegX, validKeys),
                Direction.Left => !CheckValidSocket(affectedCell.PossiblePrototypes[i].PosX, validKeys),
                Direction.Forward => !CheckValidSocket(affectedCell.PossiblePrototypes[i].NegZ, validKeys),
                Direction.Backward => !CheckValidSocket(affectedCell.PossiblePrototypes[i].PosZ, validKeys),
                Direction.Up => !CheckValidSocket(affectedCell.PossiblePrototypes[i].NegY, validKeys),
                Direction.Down => !CheckValidSocket(affectedCell.PossiblePrototypes[i].PosY, validKeys),
                _ => false
            };

            if (!shouldRemove) continue;
            affectedCell.PossiblePrototypes.RemoveAtSwapBack(i);
            changed = true;
        }

        return affectedCell.PossiblePrototypes;
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
                int thing = 1 + Mathf.FloorToInt(index / (float)(gridSizeX * gridSizeY));
                if (index + gridSizeX < gridSizeX * gridSizeY * thing)
                {
                    directIndex = index + gridSizeX;
                    return true;
                }
                break;
            case Direction.Down:
                int thing1 = 1 + Mathf.FloorToInt(index / (float)(gridSizeX * gridSizeY));
                if (index - gridSizeX >= gridSizeX * gridSizeY * (thing1 - 1))
                {
                    directIndex = index - gridSizeX;
                    return true;
                }
                break;
            case Direction.Forward:
                if (index + gridSizeX * gridSizeY < cells.Count)
                {
                    directIndex = index + gridSizeX * gridSizeY;
                    return true;
                }
                break;
            case Direction.Backward:
                if (index - gridSizeX * gridSizeY >= 0)
                {
                    directIndex = index - gridSizeX * gridSizeY;
                    return true;
                }
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
            int thing = 1 + Mathf.FloorToInt(index / (float)(gridSizeX * gridSizeY));
            if (index + gridSizeX < gridSizeX * gridSizeY * thing)
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
    
    private static bool CheckValidSocket(string key, List<string> validKeys)
    {
        if (key.Contains('v')) // Ex. v0_0
        {
            return validKeys.Contains(key);
        }

        if (key.Contains('s')) // Ex. 0s
        {
            return validKeys.Contains(key);
        }

        if (key.Contains('f')) // Ex. 0f
        {
            return validKeys.Contains(key.Replace("f", ""));
        }

        // Ex. 0
        string keyf = key + 'f';
        return validKeys.Contains(keyf);
    }
    
    private Vector3Int GetCords(int index)
    {
        int x = index / (gridSizeZ * gridSizeY);
        int y = (index / gridSizeZ) % gridSizeY;
        int z = index % gridSizeZ;

        return new Vector3Int(x, y, z);
    }

    public int GetIndex(int x, int y, int z) => (x * gridSizeY * gridSizeZ) + (y * gridSizeZ) + z;

    private bool LoadPrototypeData()
    {
        if (prototypeInfo is null)
        {
            Debug.LogError("Please enter prototype reference");
            return false;
        }

        prototypes = new List<PrototypeData>(prototypeInfo.Prototypes);
        
        bottomPrototypes.Clear();
        for (int i = 0; i < prototypes.Count; i++)
        {
            if (prototypeInfo.NotAllowedForBottom.Contains(i))
            {
                continue;
            }

            bottomPrototypes.Add(prototypes[i]);
        }

        return prototypes.Count > 0;
    }

    private void LoadCells()
    {
        emptyPrototype = new PrototypeData(new MeshWithRotation(null, 0), "-1s", "-1s", "-1s", "-1s", "-1s", "-1s", 20, Array.Empty<int>());

        for (int z = 0; z < gridSizeZ; z++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                for (int x = 0; x < gridSizeX; x++)
                {
                    Vector3 pos = new Vector3(x * gridScale.x, y * gridScale.y, z * gridScale.z);

                    bool isBottom = y == 0;
                    List<PrototypeData> prots = new(isBottom ? bottomPrototypes : prototypes);

                    List<Direction> directions = GetAdjacentSides(x, y, z, gridSizeX, gridSizeY, gridSizeZ, 1);
                    foreach (Direction direction in directions)
                    {
                        for (int i = prots.Count - 1; i >= 0; i--)
                        {
                            bool shouldRemove = direction switch
                            {
                                Direction.Right => prots[i].PosX != "-1s",
                                Direction.Left => prots[i].NegX != "-1s", 
                                Direction.Backward => prots[i].NegZ != "-1s",
                                Direction.Forward => prots[i].PosZ != "-1s",
                                Direction.Up => prots[i].PosY != "-1s",
                                Direction.Down => prots[i].NegY != "-1s",
                                _ => false
                            };

                            if (shouldRemove) prots.RemoveAt(i);
                        }
                    }

                    cells.Add(new Cell(false, pos + OriginPosition, prots));
                }
            }
        }
    }

    private static List<Direction> GetAdjacentSides(int x, int y, int z, int width, int height, int depth, int minY = 0)
    {
        // Validate the input dimensions
        if (width <= 0 || height <= 0 || depth <= 0)
            throw new ArgumentException("Invalid dimensions for the 3D array.");

        var directions = new List<Direction>();

        // Check each adjacent direction based on boundaries and add if there is NO adjacent cell
        if (x + 1 >= width && y >= minY)
            directions.Add(Direction.Right);
        if (x - 1 < 0 && y >= minY)
            directions.Add(Direction.Left);
        if (y + 1 >= height)
            directions.Add(Direction.Up);
        if (y - 1 < 0)
            directions.Add(Direction.Down);
        if (z + 1 >= depth && y >= minY)
            directions.Add(Direction.Forward);
        if (z - 1 < 0 && y >= minY)
            directions.Add(Direction.Backward);


        return directions;
    }

    private GameObject GenerateMesh(Vector3 position, PrototypeData prototypeData, float scale = 1)
    {
        if (prototypeData.MeshRot.Mesh is null)
        {
            return null;
        }

        GameObject gm = new GameObject();
        gm.AddComponent<MeshFilter>().mesh = prototypeData.MeshRot.Mesh;
        gm.AddComponent<MeshRenderer>().SetMaterials(materialData.GetMaterials(prototypeData.MaterialIndexes));

        gm.transform.position = position;
        gm.transform.rotation = Quaternion.Euler(0, 90 * prototypeData.MeshRot.Rot, 0);
        parentTransform ??= new GameObject("Wave Function Parent").transform;
        gm.transform.SetParent(parentTransform, true);
        
        gm.transform.localScale = GridScale / 2 * scale;

        return gm;
    }

    public void Clear()
    {
        for (int i = 0; i < spawnedMeshes.Count; i++)
        {
            Object.DestroyImmediate(spawnedMeshes[i]);
        }

        spawnedMeshes.Clear();
        cells.Clear();
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

    private Cell GetCellAtIndex(int x, int y, int z)
    {
        return cells[GetIndex(x, y, z)];
    }

    public Vector3Int GetIndexAtPosition(Vector3 pos)
    {
        Vector3Int index = new Vector3Int(Mathf.FloorToInt(pos.x / gridScale.x), Mathf.FloorToInt(pos.y / gridScale.y), Mathf.FloorToInt(pos.z / gridScale.z));
        return index;
    }

    public void SetGridSize(Vector3Int size)
    {
        gridSizeX = size.x;
        gridSizeY = size.y;
        gridSizeZ = size.z;
    }

    #endregion
    
    #region Debug

    public void DrawGizmos()
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

    private void HidePossiblePrototypes()
    {
        for (int i = 0; i < spawnedPossibilites.Count; i++)
        {
            Object.DestroyImmediate(spawnedPossibilites[i]);
        }

        spawnedPossibilites.Clear();
    }

    #endregion
}

[Serializable]
public struct MeshWithRotation : IEquatable<MeshWithRotation>
{
    public Mesh Mesh;
    public int Rot;

    public MeshWithRotation(Mesh mesh, int rot)
    {
        Mesh = mesh;
        Rot = rot;
    }

    public bool Equals(MeshWithRotation other)
    {
        return Equals(Mesh, other.Mesh) && Rot == other.Rot;
    }

    public override bool Equals(object obj)
    {
        return obj is MeshWithRotation other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Mesh, Rot);
    }
}

[Serializable]
public struct Cell
{
    public bool Collapsed;
    public bool Buildable;

    public Vector3 Position;

    public List<PrototypeData> PossiblePrototypes; // SHOULD BE AN ARRAY, 
    // SHOULD PROBABLY DIRTY CACHE THE TOTAL WEIGHT

    public Cell(bool collapsed, Vector3 position, List<PrototypeData> possiblePrototypes, bool buildable = true)
    {
        Collapsed = collapsed;
        Position = position;
        PossiblePrototypes = possiblePrototypes;
        Buildable = buildable;
    }
}

public struct Node
{
    public readonly bool Walkable;
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
    Backward
}

public static class WaveFunctionUtility
{
    public static Direction OppositeDirection(Direction direction)
    {
        return direction switch
        {
            Direction.Right => Direction.Left,
            Direction.Left => Direction.Right,
            Direction.Up => Direction.Down,
            Direction.Down => Direction.Up,
            Direction.Forward => Direction.Backward,
            Direction.Backward => Direction.Forward,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };
    }
}
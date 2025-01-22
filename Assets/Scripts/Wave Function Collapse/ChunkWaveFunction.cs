using Object = UnityEngine.Object;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Collections;
using UnityEngine;
using System;

[Serializable]
public class ChunkWaveFunction
{
    [Title("Grid Size")]
    [SerializeField]
    private Vector3 gridScale;

    [Title("Mesh")]
    [SerializeField]
    private MaterialData materialData;

    [Title("Prototypes")]
    [SerializeField]
    private PrototypeInfoCreator prototypeInfo;
    
    private List<Chunk> chunks = new List<Chunk>();

    private readonly List<GameObject> spawnedMeshes = new List<GameObject>();
    private List<PrototypeData> prototypes = new List<PrototypeData>();
    private readonly Stack<ChunkIndex> cellStack = new Stack<ChunkIndex>();

    private Transform parentTransform;
    private PrototypeData emptyPrototype;

    public Vector3 GridScale => gridScale;
    
    public Vector3 OriginPosition { get; set; }
    public List<PrototypeData> Prototypes => prototypes;
    
    public Cell this[ChunkIndex index]
    {
        get => chunks[index.Index][index.CellIndex];
        set => chunks[index.Index][index.CellIndex] = value;
    }

    public bool Load()
    {
        Clear();

        if (LoadPrototypeData()) return true;
        
        Debug.LogError("No prototype data found");
        return false;
    }

    public void LoadChunk(Vector3 position, Vector3Int size)
    {
        Chunk[] adjacentChunks = GetAdjacentChunks(position, size);
        Chunk chunk = new Chunk(size.x, size.y, size.z, chunks.Count, position, adjacentChunks, gridScale);
        chunk.LoadCells(prototypes, gridScale);
        chunks.Add(chunk);
        
        for (int i = 0; i < adjacentChunks.Length; i++)
        {
            if (adjacentChunks[i] == null) continue;
            
            adjacentChunks[i].SetAdjacentChunk(chunk, WaveFunctionUtility.OppositeDirection((Direction)i), prototypes);
        }
    }

    private Chunk[] GetAdjacentChunks(Vector3 position, Vector3Int size)
    {
        Chunk[] adjacentChunks = new Chunk[6];
        
        // Calculate positions of the adjacent chunks
        Vector3?[] offsets = 
        {
            new Vector3(size.x * gridScale.x, 0, 0),   // Right
            new Vector3(-size.x * gridScale.x, 0, 0),  // Left
            new Vector3(0, size.y * gridScale.y, 0),   // Up
            new Vector3(0, -size.y * gridScale.y, 0),  // Down
            new Vector3(0, 0, size.z * gridScale.z),   // Forward
            new Vector3(0, 0, -size.z * gridScale.z)   // Backward
        };
        
        foreach (Chunk chunk in chunks)
        {
            for (int i = 0; i < offsets.Length; i++)
            {
                if (!offsets[i].HasValue) continue;
                if (!chunk.IsWithinBounds(position + offsets[i].Value)) continue;
                
                adjacentChunks[i] = chunk;
                offsets[i] = null;
                break;
            }
        }
        
        return adjacentChunks;
    }

    public void Iterate()
    {
        ChunkIndex index = GetLowestEntropyIndex();

        PrototypeData chosenPrototype = Collapse(this[index]);
        SetCell(index, chosenPrototype);

        Propagate();
    }

    public ChunkIndex GetLowestEntropyIndex()
    {
        float lowestEntropy = 10000;
        ChunkIndex index = new ChunkIndex();

        for (int i = 0; i < chunks.Count; i++)
        for (int x = 0; x < chunks[i].Cells.GetLength(0); x++)
        for (int y = 0; y < chunks[i].Cells.GetLength(1); y++)
        for (int z = 0; z < chunks[i].Cells.GetLength(2); z++)
        {
            Cell cell = chunks[i].Cells[x, y, z];
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
                if (distFromAverage < 1.0f) distFromAverage *= distFromAverage; // Because of using the percentage as a distance, smaller weights weigh more, so this is to try to correct that.

                possibleMeshAmount += Mathf.Lerp(1, 0, Mathf.Abs(distFromAverage));
            }

            if (possibleMeshAmount >= lowestEntropy) continue;
            
            lowestEntropy = possibleMeshAmount;
            index = new ChunkIndex(i, new Vector3Int(x, y, z));
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

    public void SetCell(ChunkIndex index, PrototypeData chosenPrototype)
    {
        this[index] = new Cell(true, this[index].Position, new List<PrototypeData>() { chosenPrototype });
        cellStack.Push(index);

        GameObject spawned = GenerateMesh(this[index].Position, chosenPrototype);
        if (spawned != null)
        {
            spawnedMeshes.Add(spawned);
        }
    }

    public void Propagate()
    {
        while (cellStack.TryPop(out ChunkIndex chunkIndex))
        {
            Cell changedCell = this[chunkIndex];

            List<ChunkIndex> neighbours = chunks[chunkIndex.Index].GetAdjacentCells(chunkIndex.CellIndex, out List<Direction> directions);

            for (int i = 0; i < neighbours.Count; i++)
            {
                Cell neighbour = this[neighbours[i]];
                List<PrototypeData> constrainedPrototypes = Constrain(changedCell, neighbour, directions[i], out bool changed);

                if (changed)
                {
                    this[neighbours[i]] = new Cell(neighbour.Collapsed, neighbour.Position, constrainedPrototypes);
                    cellStack.Push(neighbours[i]);
                } 
            }
        }
    }

    private List<PrototypeData> Constrain(Cell changedCell, Cell affectedCell, Direction direction, out bool changed)
    {
        if (affectedCell.Collapsed)
        {
            changed = false;
            return new List<PrototypeData>(affectedCell.PossiblePrototypes);
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

    private bool LoadPrototypeData()
    {
        if (prototypeInfo is null)
        {
            Debug.LogError("Please enter prototype reference");
            return false;
        }

        prototypes = prototypeInfo.Prototypes;
        return prototypes.Count > 0;
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
        chunks.Clear();
    }

    #region Debug

    public void DrawGizmos()
    {
        if (!UnityEditor.EditorApplication.isPlaying)
        {
            return;
        }

        foreach (var chunk in chunks)
        {
            foreach (var cell in chunk.Cells)
            {
                Vector3 pos = cell.Position;
                Gizmos.color = cell.Buildable ? Color.white : Color.red;
                Gizmos.DrawWireCube(pos, new Vector3(GridScale.x, GridScale.y, GridScale.z) * 0.75f);
            }
        }
    }
    
    #endregion
}

public struct ChunkIndex
{
    public readonly int Index;
    public readonly Vector3Int CellIndex;

    public ChunkIndex(int index, Vector3Int cellIndex)
    {
        Index = index;
        CellIndex = cellIndex;
    }
}

[Serializable]
public class Chunk
{
    // In the Following order: Right, Left, Up, Down, Forward, Backward
    public readonly Chunk[] AdjacentChunks;
    public readonly Cell[,,] Cells;

    public readonly Vector3 OriginPosition;
    public int ChunkIndex;
    
    private readonly int width;
    private readonly int height;
    private readonly int depth;

    private readonly Vector3 min;
    private readonly Vector3 max;

    private static readonly Vector3Int[] Directions = {
        new Vector3Int(1, 0, 0),  // Right
        new Vector3Int(-1, 0, 0), // Left
        new Vector3Int(0, 1, 0),  // Up
        new Vector3Int(0, -1, 0), // Down
        new Vector3Int(0, 0, 1),  // Forward
        new Vector3Int(0, 0, -1)  // Backward
    };

    public Cell this[Vector3Int index]
    {
        get => Cells[index.x, index.y, index.z];
        set => Cells[index.x, index.y, index.z] = value;
    }

    public Chunk(int _width, int _height, int _depth, int chunkIndex, Vector3 originPosition, Chunk[] adjacentChunks, Vector3 gridScale)
    {
        OriginPosition = originPosition;
        AdjacentChunks = adjacentChunks;
        width = _width;
        height = _height;
        depth = _depth;
        ChunkIndex = chunkIndex;

        min = originPosition;
        max = originPosition + new Vector3(width * gridScale.x, height * gridScale.y, depth * gridScale.z);
        
        Cells = new Cell[width, height, depth];
    }
    
    public void LoadCells(List<PrototypeData> prototypes, Vector3 gridScale)
    {
        for (int z = 0; z < depth; z++)
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            Vector3Int index = new Vector3Int(x, y, z);
            Vector3 pos = new Vector3(x * gridScale.x, y * gridScale.y, z * gridScale.z);

            List<PrototypeData> prots = new List<PrototypeData>(prototypes);
            List<Direction> directions = GetAdjacentSides(index);
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

            Cells[x, y, z] = new Cell(false, pos + OriginPosition, prots);
        }
    }

    public List<ChunkIndex> GetAdjacentCells(Vector3Int index, out List<Direction> directions)
    {
        List<ChunkIndex> adjacentCells = new List<ChunkIndex>();
        directions = new List<Direction>();

        for (int i = 0; i < Directions.Length; i++)
        {
            Vector3Int neighborIndex = index + Directions[i];

            // Check if neighbor is within the bounds of the current chunk
            if (IsWithinBounds(neighborIndex))
            {
                adjacentCells.Add(new ChunkIndex(ChunkIndex, neighborIndex));
                directions.Add((Direction)i);
            }
            else
            {
                // If on the edge, check for an adjacent chunk
                if (AdjacentChunks[i] == null) continue;
                
                Vector3Int adjIndex = WrapIndexToAdjacentChunk(neighborIndex, i);
                adjacentCells.Add(new ChunkIndex(AdjacentChunks[i].ChunkIndex, adjIndex));
                directions.Add((Direction)i);
            }
        }

        return adjacentCells;
    }
    
    /// <summary>
    /// Gets the directions of the index that are invalid
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public List<Direction> GetAdjacentSides(Vector3Int index)
    {
        List<Direction> adjacentDirections = new List<Direction>();

        for (int i = 0; i < Directions.Length; i++)
        {
            Vector3Int neighborIndex = index + Directions[i];

            // Check if neighbor is within the bounds of the current chunk
            if (AdjacentChunks[i] == null && !IsWithinBounds(neighborIndex))
            {
                adjacentDirections.Add((Direction)i);
            }
        }

        return adjacentDirections;
    }

    private bool IsWithinBounds(Vector3Int index) =>
        index.x >= 0 && index.x < width &&
        index.y >= 0 && index.y < height &&
        index.z >= 0 && index.z < depth;
    
    public bool IsWithinBounds(Vector3 position) =>
        position.x >= min.x && position.x < max.x &&
        position.y >= min.y && position.y < max.y &&
        position.z >= min.z && position.z < max.z;
    

    private Vector3Int WrapIndexToAdjacentChunk(Vector3Int index, int direction)
    {
        return direction switch
        {
            0 => new Vector3Int(0, index.y, index.z), // Right
            1 => new Vector3Int(width - 1, index.y, index.z), // Left
            2 => new Vector3Int(index.x, 0, index.z), // Up
            3 => new Vector3Int(index.x, height - 1, index.z), // Down
            4 => new Vector3Int(index.x, index.y, 0), // Forward
            5 => new Vector3Int(index.x, index.y, depth - 1),    // Backward
            _ => throw new ArgumentOutOfRangeException(nameof(direction), "Invalid direction index")
        };
    }

    public void SetAdjacentChunk(Chunk chunk, Direction direction, List<PrototypeData> prototypes)
    {
        int index = (int)direction;
        if (AdjacentChunks[index] != null)
        {
            Debug.LogError("Adjacent chunk already exists, big issue!");
            return;
        }
        
        AdjacentChunks[index] = chunk;
        UpdateCellsAlongSide(direction, prototypes);
    }

    private void UpdateCellsAlongSide(Direction sideDirection, List<PrototypeData> prototypes)
    {
        int startX = sideDirection == Direction.Right ? width - 1 : 0;
        int maxX = sideDirection == Direction.Left ? 1 : width;
        int startY = sideDirection == Direction.Up ? height - 1 : 0;
        int maxY = sideDirection == Direction.Down ? 1 : height;
        int startZ = sideDirection == Direction.Forward ? depth - 1 : 0;
        int maxZ = sideDirection == Direction.Backward ? 1 : depth;
        
        for (int z = startZ; z < maxZ; z++)
        for (int y = startY; y < maxY; y++)
        for (int x = startX; x < maxX; x++)
        {
            Vector3Int index = new Vector3Int(x, y, z);
            List<PrototypeData> prots = new List<PrototypeData>(prototypes);
            List<Direction> directions = GetAdjacentSides(index);
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

            Cells[x, y, z] = new Cell(false, this[index].Position + OriginPosition, prots);
        }
    }
}



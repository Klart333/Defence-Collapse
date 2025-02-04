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
    
    [Title("Debug")]
    [SerializeField, Sirenix.OdinInspector.ReadOnly]
    private List<Chunk> chunks;

    private List<PrototypeData> prototypes = new List<PrototypeData>();
    private List<PrototypeData> bottomPrototypes = new List<PrototypeData>();
    private readonly Stack<ChunkIndex> cellStack = new Stack<ChunkIndex>();
    private readonly Stack<GameObject> gameObjectPool = new Stack<GameObject>();

    private Transform parentTransform;
    private PrototypeData emptyPrototype;

    public List<Chunk> Chunks => chunks;
    public Vector3 GridScale => gridScale;
    public Stack<GameObject> GameObjectPool => gameObjectPool;
    
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

    public Chunk LoadChunk(Vector3 position, Vector3Int size)
    {
        Chunk[] adjacentChunks = GetAdjacentChunks(position, size);
        Chunk chunk = new Chunk(size.x, size.y, size.z, chunks.Count, position, adjacentChunks, gridScale);
        chunks.Add(chunk);
        LoadCells(chunk);
        return chunk;
    }

    public void LoadCells(Chunk chunk)
    {
        chunk.LoadCells(prototypes, bottomPrototypes, gridScale, cellStack);
        for (int i = 0; i < chunk.AdjacentChunks.Length; i++)
        {
            if (chunk.AdjacentChunks[i] == null || chunk.AdjacentChunks[i].IsClear || chunk.AdjacentChunks[i].AllCollapsed) continue;
            
            Direction oppositeDirection = WaveFunctionUtility.OppositeDirection(i);
            chunk.AdjacentChunks[i].SetAdjacentChunk(chunk, oppositeDirection, prototypes, bottomPrototypes, cellStack);
        }
    }
    
    public bool CheckChunkOverlap(Vector3 position, Vector3Int size, out Chunk chunk)
    {
        chunk = null;
        foreach (Chunk existingChunk in chunks)
        {
            // Get the bounds of the existing chunk
            Vector3 existingMin = existingChunk.OriginPosition;
            Vector3 existingMax = existingChunk.OriginPosition + Vector3.Scale(new Vector3(existingChunk.width, existingChunk.height, existingChunk.depth), gridScale);

            // Get the bounds of the incoming chunk
            Vector3 incomingMax = position + Vector3.Scale(size, gridScale);

            // Check for overlap between the AABBs
            if (position.x >= existingMax.x || incomingMax.x <= existingMin.x ||
                position.y >= existingMax.y || incomingMax.y <= existingMin.y ||
                position.z >= existingMax.z || incomingMax.z <= existingMin.z) continue;
            
            chunk = existingChunk;
            return true;
        }
        
        return false;
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
    
    public void RemoveChunk(int chunkIndex, out List<Chunk> neighbours)
    {
        neighbours = new List<Chunk>();
        Chunk chunk = chunks[chunkIndex];
        for (int i = 0; i < chunk.AdjacentChunks.Length; i++)
        {
            if (chunk.AdjacentChunks[i] == null) continue;
            
            int oppositeDirection = (int)WaveFunctionUtility.OppositeDirection(i);
            chunk.AdjacentChunks[i].AdjacentChunks[oppositeDirection] = null;
            neighbours.Add(chunk.AdjacentChunks[i]);
        }
        
        chunk.Clear(gameObjectPool);
        chunk.IsRemoved = true;
        chunks.RemoveAtSwapBack(chunkIndex);

        if (chunkIndex >= chunks.Count)
        {
            return;
        }
        chunks[chunkIndex].ChunkIndex = chunkIndex;
    }
    
    public void Iterate()
    {
        ChunkIndex index = GetLowestEntropyIndex();

        PrototypeData chosenPrototype = Collapse(this[index]);
        //Debug.Log("Collapsing: " + index + "\n ChosenPrototype: " + chosenPrototype);
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

            float possibleMeshAmount = y * 10;
            if (possibleMeshAmount > lowestEntropy) continue;
            
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
        this[index] = new Cell(true, this[index].Position, new List<PrototypeData> { chosenPrototype });
        cellStack.Push(index);

        GameObject spawned = GenerateMesh(this[index].Position, chosenPrototype);
        if (spawned is not null)
        {
            chunks[index.Index].SpawnedMeshes.Add(spawned);
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
                WaveFunctionUtility.Constrain(changedCell, neighbour, directions[i], out bool changed);

                if (changed)
                {
                    cellStack.Push(neighbours[i]);
                } 
            }
        }
    }

    private bool LoadPrototypeData()
    {
        if (prototypeInfo is null)
        {
            Debug.LogError("Please enter prototype reference");
            return false;
        }

        prototypes = new List<PrototypeData>(prototypeInfo.Prototypes);
        bottomPrototypes = new List<PrototypeData>(prototypeInfo.Prototypes);
        for (int i = prototypes.Count - 1; i >= 0; i--)
        {
            if (prototypeInfo.OnlyAllowedForBottom.Contains(i))
            {
                prototypes.RemoveAt(i);
            }
        }

        return prototypes.Count > 0;
    }

    private GameObject GenerateMesh(Vector3 position, PrototypeData prototypeData, float scale = 1)
    {
        if (prototypeData.MeshRot.Mesh == null)
        {
            return null;
        }

        GameObject gm = GetPoolObject(prototypeData);

        gm.transform.position = position;
        gm.transform.rotation = Quaternion.Euler(0, 90 * prototypeData.MeshRot.Rot, 0);
        parentTransform ??= new GameObject("Wave Function Parent").transform;
        gm.transform.SetParent(parentTransform, true);
        
        gm.transform.localScale = GridScale / 2 * scale;

        return gm;
    }

    private GameObject GetPoolObject(PrototypeData prototypeData)
    {
        if (gameObjectPool.TryPop(out GameObject gameObject))
        {
            gameObject.SetActive(true);
            gameObject.GetComponent<MeshFilter>().mesh = prototypeData.MeshRot.Mesh;
            gameObject.GetComponent<MeshRenderer>().SetMaterials(materialData.GetMaterials(prototypeData.MaterialIndexes));
            return gameObject;
        }
        
        var gm = new GameObject(prototypeData.MeshRot.Mesh.name);
        gm.AddComponent<MeshFilter>().mesh = prototypeData.MeshRot.Mesh;
        gm.AddComponent<MeshRenderer>().SetMaterials(materialData.GetMaterials(prototypeData.MaterialIndexes));    
        return gm;
    }
    

    public void Clear()
    {
        foreach (Chunk chunk in chunks)
        {
            chunk.Clear(gameObjectPool);
        }
        chunks.Clear();
    }
    
    public bool AllCollapsed()
    {
        foreach (Chunk chunk in chunks)
        {
            if (!chunk.AllCollapsed)
            {
                return false;
            }
        }

        return true;
    }

    public int GetTotalCellCount()
    {
        return chunks.Count * 8;
    }
}

public readonly struct ChunkIndex
{
    public readonly int Index;
    public readonly Vector3Int CellIndex;

    public ChunkIndex(int index, Vector3Int cellIndex)
    {
        Index = index;
        CellIndex = cellIndex;
    }

    public override string ToString()
    {
        return $"(ChunkIndex) {Index}, {CellIndex}";
    }
}

[Serializable]
public class Chunk
{
#if UNITY_EDITOR
    [SerializeField, Sirenix.OdinInspector.ReadOnly]
    private Vector3 position;
#endif
    
    // In the Following order: Right, Left, Up, Down, Forward, Backward
    public readonly Chunk[] AdjacentChunks;
    public readonly Cell[,,] Cells;

    [NonSerialized]
    public List<GameObject> SpawnedMeshes = new List<GameObject>();
    
    public readonly Vector3 OriginPosition;
    
    public readonly int width;
    public readonly int height;
    public readonly int depth;

    private readonly Vector3 min;
    private readonly Vector3 max;

    public int ChunkIndex { get; set; }
    public bool IsRemoved { get; set; }
    public bool IsClear { get; private set; } = true; 


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
        
        #if UNITY_EDITOR
        position = originPosition;
        #endif
    }

    public bool AllCollapsed
    {
        get
        {
            if (IsClear)
            {
                return false;
            }
            
            foreach (Cell cell in Cells)
            {
                if (!cell.Collapsed)
                {
                    return false;
                }
            }

            return true;
        }
    }


    public void LoadCells(List<PrototypeData> prototypes, List<PrototypeData> bottomPrototypes, Vector3 gridScale, Stack<ChunkIndex> cellStack)
    {
        IsClear = false;
        
        for (int z = 0; z < depth; z++)
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            Vector3Int index = new Vector3Int(x, y, z);
            Vector3 pos = new Vector3(x * gridScale.x, y * gridScale.y, z * gridScale.z);

            bool isBottom = AdjacentChunks[3] == null && y == 0;
            List<PrototypeData> prots = new List<PrototypeData>(isBottom ? bottomPrototypes : prototypes);
            List<Direction> directions = GetAdjacentSides(index, cellStack);
            bool changed = false;
            foreach (Direction direction in directions)
            {
                for (int i = prots.Count - 1; i >= 0; i--)
                {
                    if (prots[i].DirectionToKey(direction)[0] == '-') continue;
                    
                    prots.RemoveAtSwapBack(i);
                    changed = true;
                }
            }

            Cells[x, y, z] = new Cell(false, pos + OriginPosition, prots);
            if (changed)
            {
                cellStack.Push(new ChunkIndex(ChunkIndex, index));
            }
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
    public List<Direction> GetAdjacentSides(Vector3Int index, Stack<ChunkIndex> cellStack)
    {
        //Debug.Log("Get Adjacent Sides for : " + ChunkIndex + ", " + index);
        List<Direction> adjacentDirections = new List<Direction>();

        for (int i = 0; i < Directions.Length; i++)
        {
            Vector3Int neighborIndex = index + Directions[i];
            if (IsWithinBounds(neighborIndex)) continue;

            if (AdjacentChunks[i] == null)
            {
                adjacentDirections.Add((Direction)i);
                continue;
            }

            if (AdjacentChunks[i].IsClear) continue;

            Vector3Int adjacentIndex = WrapIndexToAdjacentChunk(index, i);
            Cell cell = AdjacentChunks[i][adjacentIndex];
            if (!cell.Collapsed || string.IsNullOrEmpty(cell.PossiblePrototypes[0].Keys[i])) continue;
            
            if (cell.PossiblePrototypes[0].Keys[(int)WaveFunctionUtility.OppositeDirection(i)][0] == '-')
            {
                adjacentDirections.Add((Direction)i);
            }
            else
            {
                cellStack.Push(new ChunkIndex(AdjacentChunks[i].ChunkIndex, adjacentIndex));
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

    public void SetAdjacentChunk(Chunk chunk, Direction direction, List<PrototypeData> prototypes, List<PrototypeData> bottomPrototypes, Stack<ChunkIndex> cellStack)
    {
        int index = (int)direction;
        if (AdjacentChunks[index] != null && AdjacentChunks[index].ChunkIndex != chunk.ChunkIndex)
        {
            Debug.LogError("Adjacent chunk already exists, big issue!");
            return;
        }
        
        AdjacentChunks[index] = chunk;
        UpdateCellsAlongSide(direction, prototypes, bottomPrototypes, cellStack);
    }

    private void UpdateCellsAlongSide(Direction sideDirection, List<PrototypeData> prototypes, List<PrototypeData> bottomPrototypes, Stack<ChunkIndex> cellStack)
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
            bool isBottom = AdjacentChunks[3] == null && y == 0;    
            List<PrototypeData> prots = new List<PrototypeData>(isBottom ? bottomPrototypes : prototypes);
            List<Direction> directions = GetAdjacentSides(index, cellStack);
            bool changed = false;
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

                    if (shouldRemove)
                    {
                        prots.RemoveAt(i);
                        changed = true;
                    }
                }
            }

            Cells[x, y, z] = new Cell(false, this[index].Position, prots);
            if (changed)
            {
                cellStack.Push(new ChunkIndex(ChunkIndex, index));
            }
        }
    }

    public void Clear(Stack<GameObject> pool)
    {
        IsClear = true;
        
        for (int i = SpawnedMeshes.Count - 1; i >= 0; i--)
        {
            SpawnedMeshes[i].SetActive(false);
            pool.Push(SpawnedMeshes[i]);
            SpawnedMeshes.RemoveAt(i);
        }
    }
}

public interface IChunkWaveFunction
{
    public ChunkWaveFunction ChunkWaveFunction { get; }
}



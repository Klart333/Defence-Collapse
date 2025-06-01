using Random = UnityEngine.Random;
using System.Collections.Generic;
using UnityEngine.Serialization;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;
using System;
using System.Linq;
using Pathfinding;

namespace WaveFunctionCollapse
{
    
    [Serializable]
    public class ChunkWaveFunction<TChunk> where TChunk : IChunk, new()
    {
        [Title("Grid Size")]
        [SerializeField]
        private Vector3 gridScale;

        [Title("Mesh")]
        [SerializeField]
        private MaterialData materialData;
        
        [SerializeField]
        private ProtoypeMeshes protoypeMeshes;

        private readonly Stack<GameObject> gameObjectPool = new Stack<GameObject>();

        private PrototypeData emptyPrototype;

        private IChunkWaveFunction<TChunk> handler;

        public ProtoypeMeshes ProtoypeMeshes => protoypeMeshes; 
        public Stack<ChunkIndex> CellStack { get; } = new Stack<ChunkIndex>();
        public Dictionary<int3, TChunk> Chunks { get; } = new Dictionary<int3, TChunk>();
        public Stack<GameObject> GameObjectPool => gameObjectPool;
        public Transform ParentTransform { get; set; }
        public Vector3 CellSize => gridScale;

        public Cell this[ChunkIndex index]
        {
            get 
            {
#if UNITY_EDITOR
                if (!Chunks.TryGetValue(index.Index, out _))
                {
                    Debug.LogError("PROBLEM! - " + index.Index);
                    return default;
                }
#endif

                return Chunks[index.Index][index.CellIndex]; 
            }
            set
            {
                if (Chunks.TryGetValue(index.Index, out TChunk chunk))
                {
                    chunk[index.CellIndex] = value; 
                    Chunks[index.Index] = chunk; // Store back modified chunk
                }
                else
                {
                    throw new KeyNotFoundException();
                }
            }
        }

        public bool Load(IChunkWaveFunction<TChunk> handler)
        {
            this.handler = handler;
            Clear();

            return true;
        }

        public TChunk LoadChunk(Vector3 position, Vector3Int size, PrototypeInfoData prototypeInfo, bool useSideConstraints = true)
        {
            int3 index = ChunkWaveUtility.GetDistrictIndex3(position, handler.ChunkScale);
            position = ChunkWaveUtility.GetPosition(index, handler.ChunkScale);
            TChunk[] adjacentChunks = GetAdjacentChunks(index);
            TChunk chunk = new TChunk();
            chunk.Construct(size.x, size.y, size.z, index, position, adjacentChunks.Cast<IChunk>().ToArray(), useSideConstraints);
            Chunks.Add(index, chunk);
            LoadCells(chunk, prototypeInfo);
            return chunk;
        }
        
        public TChunk LoadChunk(int3 index, Vector3Int size, PrototypeInfoData prototypeInfo, bool useSideConstraints = true)
        {
            Vector3 position = ChunkWaveUtility.GetPosition(index, handler.ChunkScale);
            TChunk[] adjacentChunks = GetAdjacentChunks(index);
            TChunk chunk = new TChunk();
            chunk.Construct(size.x, size.y, size.z, index, position, adjacentChunks.Cast<IChunk>().ToArray(), useSideConstraints);            Chunks.Add(index, chunk);
            LoadCells(chunk, prototypeInfo);
            return chunk;
        }

        public TChunk LoadChunk(int3 index, TChunk chunk)
        {
            Chunks.Add(index, chunk);
            UpdateAdjacentChunks(chunk);
            return chunk;
        }

        public void LoadCells(TChunk chunk, PrototypeInfoData prototypeInfo)
        {
            chunk.LoadCells(prototypeInfo, gridScale, CellStack);
            UpdateAdjacentChunks(chunk);
        }

        private void UpdateAdjacentChunks(TChunk chunk)
        {
            for (int i = 0; i < chunk.AdjacentChunks.Length; i++)
            {
                if (chunk.AdjacentChunks[i] == null || chunk.AdjacentChunks[i].IsClear) continue;
                
                Direction oppositeDirection = WaveFunctionUtility.OppositeDirection(i);
                if (chunk.AdjacentChunks[i].AllCollapsed)
                {
                    chunk.AdjacentChunks[i].AdjacentChunks[(int)oppositeDirection] = chunk;
                    continue;
                }
                
                chunk.AdjacentChunks[i].SetAdjacentChunk(chunk, oppositeDirection, CellStack);
            }
        }

        public TChunk[] GetAdjacentChunks(int3 index)
        {
            TChunk[] adjacentChunks = new TChunk[6];

            for (int i = 0; i < 6; i++)
            {
                int3 adjacentIndex = index + ChunkWaveUtility.Directions[i];
                if (Chunks.TryGetValue(adjacentIndex, out TChunk chunk))
                {
                    adjacentChunks[i] = chunk;
                }
            }

            return adjacentChunks;
        }

        public void RemoveChunk(int3 chunkIndex, out List<TChunk> neighbours)
        {
            neighbours = new List<TChunk>();
            if (!Chunks.TryGetValue(chunkIndex, out TChunk chunk))
            {
                Debug.LogError($"Chunk {chunkIndex} to remove not found");
                return;
            }

            for (int i = 0; i < chunk.AdjacentChunks.Length; i++)
            {
                if (chunk.AdjacentChunks[i] == null) continue;

                int oppositeDirection = (int)WaveFunctionUtility.OppositeDirection(i);
                chunk.AdjacentChunks[i].AdjacentChunks[oppositeDirection] = null;
                neighbours.Add((TChunk)chunk.AdjacentChunks[i]);
            }

            chunk.Clear(gameObjectPool);
            chunk.IsRemoved = true;
            Chunks.Remove(chunkIndex);
        }
        
        public void RemoveChunk(int3 chunkIndex)
        {
            if (!Chunks.TryGetValue(chunkIndex, out TChunk chunk))
            {
                Debug.LogError("Chunk to remove not found");
                return;
            }

            for (int i = 0; i < chunk.AdjacentChunks.Length; i++)
            {
                if (chunk.AdjacentChunks[i] == null) continue;

                int oppositeDirection = (int)WaveFunctionUtility.OppositeDirection(i);
                chunk.AdjacentChunks[i].AdjacentChunks[oppositeDirection] = null;
            }

            chunk.Clear(gameObjectPool);
            chunk.IsRemoved = true;
            Chunks.Remove(chunkIndex);
        }

        public Cell Iterate()
        {
            ChunkIndex index = GetLowestEntropyIndex();

            PrototypeData chosenPrototype = Collapse(this[index]);
            //Debug.Log("Collapsing: " + index + "\n ChosenPrototype: " + chosenPrototype);
            SetCell(index, chosenPrototype);

            Propagate();
            return this[index];
        }
        
        public ChunkIndex Iterate(TChunk chunk)
        {
            ChunkIndex index = GetLowestEntropyIndex(chunk);

            PrototypeData chosenPrototype = Collapse(this[index]);
            //Debug.Log("Collapsing: " + index.CellIndex + "\n ChosenPrototype: " + chosenPrototype);
            SetCell(index, chosenPrototype);

            Propagate();
            return index;
        }

        public ChunkIndex GetLowestEntropyIndex()
        {
            float lowestEntropy = 10000;
            ChunkIndex index = new ChunkIndex();

            foreach (TChunk chunk in Chunks.Values)
            {
                for (int x = 0; x < chunk.Cells.GetLength(0); x++)
                for (int y = 0; y < chunk.Cells.GetLength(1); y++)
                for (int z = 0; z < chunk.Cells.GetLength(2); z++)
                {
                    Cell cell = chunk.Cells[x, y, z];
                    if (cell.Collapsed)
                    {
                        continue;
                    }

                    float cellEntropy = cell.Position.y * 10;
                    if (cellEntropy > lowestEntropy) continue;

                    cellEntropy += WaveFunctionUtility.CalculateEntropy(cell);
                    if (cellEntropy >= lowestEntropy) continue;

                    lowestEntropy = cellEntropy;
                    index = new ChunkIndex(chunk.ChunkIndex, new int3(x, y, z));
                }
            }

            return index;
        }
        
        public ChunkIndex GetLowestEntropyIndex(TChunk chunk)
        {
            float lowestEntropy = 10000;
            ChunkIndex index = new ChunkIndex();
            
            for (int x = 0; x < chunk.Cells.GetLength(0); x++)
            for (int y = 0; y < chunk.Cells.GetLength(1); y++)
            for (int z = 0; z < chunk.Cells.GetLength(2); z++)
            {
                Cell cell = chunk.Cells[x, y, z];
                if (cell.Collapsed)
                {
                    continue;
                }

                float cellEntropy = y * 10;
                if (cellEntropy > lowestEntropy) continue;

                cellEntropy += WaveFunctionUtility.CalculateEntropy(cell);
                if (cellEntropy >= lowestEntropy) continue;

                lowestEntropy = cellEntropy;
                index = new ChunkIndex(chunk.ChunkIndex, new int3(x, y, z));
            }
            return index;
        }
        
        public ChunkIndex? GetLowestEntropyIndex(IEnumerable<TChunk> chunks)
        {
            float lowestEntropy = 10000;
            ChunkIndex? index = null;

            foreach (TChunk chunk in chunks)
            {
                for (int x = 0; x < chunk.Cells.GetLength(0); x++)
                for (int y = 0; y < chunk.Cells.GetLength(1); y++)
                for (int z = 0; z < chunk.Cells.GetLength(2); z++)
                {
                    Cell cell = chunk.Cells[x, y, z];
                    if (cell.Collapsed)
                    {
                        continue;
                    }

                    float cellEntropy = y * 10;
                    if (cellEntropy > lowestEntropy) continue;

                    cellEntropy += WaveFunctionUtility.CalculateEntropy(cell);
                    if (cellEntropy >= lowestEntropy) continue;

                    lowestEntropy = cellEntropy;
                    index = new ChunkIndex(chunk.ChunkIndex, new int3(x, y, z));
                }
            }
            
            return index;
        }
        
        public ChunkIndex GetLowestEntropyIndex(IEnumerable<ChunkIndex> indexes)
        {
            float lowestEntropy = 10000;
            ChunkIndex index = new ChunkIndex();
            foreach (ChunkIndex chunkIndex in indexes)
            {
                Cell cell = this[chunkIndex];
                if (cell.Collapsed)
                {
                    continue;
                }

                float cellEntropy = WaveFunctionUtility.CalculateEntropy(cell);
                if (cellEntropy >= lowestEntropy) continue;

                lowestEntropy = cellEntropy;
                index = chunkIndex;
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
            float randomIndex = Random.Range(0, totalCount);
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
            Vector3 position = this[index].Position;
            this[index] = new Cell(true, position, new List<PrototypeData> { chosenPrototype });
            CellStack.Push(index);

            GameObject spawned = GenerateMesh(position, chosenPrototype);
            if (spawned is not null)
            {
                Chunks[index.Index].SpawnedMeshes.Add(spawned);
            }
        }

        public void Propagate()
        {
            while (CellStack.TryPop(out ChunkIndex chunkIndex))
            {
                if (!Chunks.TryGetValue(chunkIndex.Index, out TChunk chunk)) continue;
                
                List<ChunkIndex> neighbours = chunk.GetAdjacentCells(chunkIndex.CellIndex, out List<Direction> directions);
                Cell changedCell = this[chunkIndex];

                for (int i = 0; i < neighbours.Count; i++)
                {
                    Cell neighbour = this[neighbours[i]];
                    WaveFunctionUtility.Constrain(changedCell, neighbour, directions[i], out bool changed);

                    if (changed)
                    {
                        CellStack.Push(neighbours[i]);
                    }
                }
            }
        }

        public GameObject GenerateMesh(Vector3 position, PrototypeData prototypeData, float scale = 1)
        {
            if (prototypeData.MeshRot.MeshIndex == -1)
            {
                return null;
            }

            GameObject gm = GetPoolObject(prototypeData);

            gm.transform.position = position;
            gm.transform.rotation = Quaternion.Euler(0, 90 * prototypeData.MeshRot.Rot, 0);
            ParentTransform ??= new GameObject("Wave Function Parent").transform;
            gm.transform.SetParent(ParentTransform, true);

            gm.transform.localScale = CellSize / 2 * scale;

            return gm;
        }

        private GameObject GetPoolObject(PrototypeData prototypeData)
        {
            Mesh mesh = protoypeMeshes[prototypeData.MeshRot.MeshIndex];
            if (gameObjectPool.TryPop(out GameObject gameObject))
            {
                gameObject.SetActive(true);
                gameObject.GetComponent<MeshFilter>().mesh = mesh;
                gameObject.GetComponent<MeshRenderer>().SetMaterials(materialData.GetMaterials(prototypeData.MaterialIndexes));
                return gameObject;
            }

            var gm = new GameObject(mesh.name);
            gm.AddComponent<MeshFilter>().mesh = mesh;
            gm.AddComponent<MeshRenderer>().SetMaterials(materialData.GetMaterials(prototypeData.MaterialIndexes));
            return gm;
        }
        
        public void Clear()
        {
            foreach (TChunk chunk in Chunks.Values)
            {
                chunk.Clear(gameObjectPool);
            }

            Chunks.Clear();
        }

        public bool AllCollapsed()
        {
            foreach (TChunk chunk in Chunks.Values)
            {
                if (!chunk.AllCollapsed)
                {
                    return false;
                }
            }

            return true;
        }
        
        public bool AllCollapsed(List<TChunk> chunks)
        {
            foreach (TChunk chunk in chunks)
            {
                if (!chunk.AllCollapsed)
                {
                    return false;
                }
            }
            
            return true;
        }
    }

    public struct ChunkIndex : IEquatable<ChunkIndex>
    {
        public int3 Index;
        public int3 CellIndex;

        public ChunkIndex(int3 index, int3 cellIndex)
        {
            Index = index;
            CellIndex = cellIndex;
        }

        public override string ToString()
        {
            return $"(ChunkIndex) {Index}, {CellIndex}";
        }

        public bool Equals(ChunkIndex other)
        {
            return Index.Equals(other.Index) && CellIndex.Equals(other.CellIndex);
        }

        public override bool Equals(object obj)
        {
            return obj is ChunkIndex other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Index, CellIndex);
        }
    }

    [Serializable]
    public class Chunk : IChunk
    {
#if UNITY_EDITOR
        [FormerlySerializedAs("position")]
        [SerializeField, Sirenix.OdinInspector.ReadOnly]
        private Vector3 editorOnlyPosition;
#endif
        public event Action OnCleared;
        
        // KEEP A SEPARATE ARRAY OF SPAWNED MESHES SO THAT I CAN DO VISUAL STUFF
        
        // In the Following order: Right, Left, Up, Down, Forward, Backward
        public IChunk[] AdjacentChunks { get; private set; }
        public Cell[,,] Cells { get; private set; }

        public List<GameObject> SpawnedMeshes { get; } = new List<GameObject>();

        public PrototypeInfoData PrototypeInfoData { get; set; }
        public bool UseSideConstraints { get; private set; }
        public bool IsClear { get; private set; } = true;
        public Vector3 Position { get; private set;}
        public int Height { get; private set;}
        public int Depth { get; private set;}
        public int Width { get; private set;}
        public int3 ChunkIndex { get; set; }
        public bool IsRemoved { get; set; }
        
        public Vector3Int ChunkSize => new Vector3Int(Width, Height, Depth);

        public bool IsTop => AdjacentChunks[2] == null;
        public Cell this[int3 index]
        {
            get => Cells[index.x, index.y, index.z];
            set => Cells[index.x, index.y, index.z] = value;
        }

        public IChunk Construct(int _width, int _height, int _depth, int3 chunkIndex, Vector3 position, IChunk[] adjacentChunks, bool sideConstraints)
        {
            Position = position;
            AdjacentChunks = adjacentChunks;
            Width = _width;
            Height = _height;
            Depth = _depth;
            ChunkIndex = chunkIndex;
            UseSideConstraints = sideConstraints;

            Cells = new Cell[Width, Height, Depth];

#if UNITY_EDITOR
            editorOnlyPosition = position;
#endif

            return this;
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

        public void LoadCells(PrototypeInfoData prototypeInfoData, Vector3 gridScale, Stack<ChunkIndex> cellStack)
        {
            PrototypeInfoData = prototypeInfoData;
            IsClear = false;

            for (int z = 0; z < Depth; z++)
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                int3 index = new int3(x, y, z);
                Vector3 pos = new Vector3(x * gridScale.x, y * gridScale.y, z * gridScale.z);

                bool isBottom = AdjacentChunks[3] == null && y == 0;
                List<PrototypeData> prots = new List<PrototypeData>(isBottom ? prototypeInfoData.Prototypes : prototypeInfoData.NotBottomPrototypes);
                if (UseSideConstraints)
                {
                    ConstrainBySides(index, prots);
                }
                else
                {
                    (this as IChunk).GetInvalidAdjacentSides(index, cellStack); // Still needs to update in case adjacent chunk is collapsed
                }

                Cells[x, y, z] = new Cell(false, pos + Position, prots);
            }

            void ConstrainBySides(int3 index, List<PrototypeData> prots)
            {
                List<Direction> directions = (this as IChunk).GetInvalidAdjacentSides(index, cellStack);
                bool changed = false;
                foreach (Direction direction in directions)
                {
                    for (int i = prots.Count - 1; i >= 0; i--)
                    {
                        if (prots[i].DirectionToKey(direction) == -1) continue;

                        prots.RemoveAtSwapBack(i);
                        changed = true;
                    }
                }
                    
                if (changed)
                {
                    cellStack.Push(new ChunkIndex(ChunkIndex, index));
                }
            }
        }

        public void SetAdjacentChunk(IChunk chunk, Direction direction, Stack<ChunkIndex> cellStack)
        {
            int index = (int)direction;
            if (AdjacentChunks[index] != null && !AdjacentChunks[index].ChunkIndex.Equals(chunk.ChunkIndex))
            {
                Debug.LogError("Adjacent chunk already exists, big issue!");
                return;
            }

            AdjacentChunks[index] = chunk;
            UpdateCellsAlongSide(direction, cellStack);
        }

        private void UpdateCellsAlongSide(Direction sideDirection, Stack<ChunkIndex> cellStack)
        {
            int startX = sideDirection == Direction.Right ? Width - 1 : 0;
            int maxX = sideDirection == Direction.Left ? 1 : Width;
            int startY = sideDirection == Direction.Up ? Height - 1 : 0;
            int maxY = sideDirection == Direction.Down ? 1 : Height;
            int startZ = sideDirection == Direction.Forward ? Depth - 1 : 0;
            int maxZ = sideDirection == Direction.Backward ? 1 : Depth;

            for (int z = startZ; z < maxZ; z++)
            for (int y = startY; y < maxY; y++)
            for (int x = startX; x < maxX; x++)
            {
                int3 index = new int3(x, y, z);
                bool isBottom = AdjacentChunks[3] == null && y == 0;
                List<PrototypeData> prots = new List<PrototypeData>(isBottom ? PrototypeInfoData.Prototypes : PrototypeInfoData.NotBottomPrototypes);
                if (UseSideConstraints)
                {
                    ConstrainBySides(index, prots);
                }

                Cells[x, y, z] = new Cell(false, this[index].Position, prots);
            }
            
            void ConstrainBySides(int3 index, List<PrototypeData> prots)
            {
                List<Direction> directions = (this as IChunk).GetInvalidAdjacentSides(index, cellStack);
                bool changed = false;
                foreach (Direction direction in directions)
                {
                    for (int i = prots.Count - 1; i >= 0; i--)
                    {
                        if (prots[i].DirectionToKey(direction) == -1) continue;

                        prots.RemoveAtSwapBack(i);
                        changed = true;
                    }
                }
                
                if (changed)
                {
                    cellStack.Push(new ChunkIndex(ChunkIndex, index));
                }
            }
        }

        public void Clear(Stack<GameObject> pool)
        {
            IsClear = true;

            ClearSpawnedMeshes(pool);
            
            OnCleared?.Invoke();
        }

        public void ClearSpawnedMeshes(Stack<GameObject> pool)
        {
            for (int i = SpawnedMeshes.Count - 1; i >= 0; i--)
            {
                SpawnedMeshes[i].SetActive(false);
                pool.Push(SpawnedMeshes[i]);
                SpawnedMeshes.RemoveAt(i);
            }
        }

        /// <remarks>Does not use height</remarks>>
        public bool ContainsPoint(Vector3 point, Vector3 scale)
        {
            return point.x < Position.x + Width * scale.x && point.x > Position.x
                && point.z < Position.z + Depth * scale.z && point.z > Position.z;
        }
    }

    public interface IChunk
    {
        public PrototypeInfoData PrototypeInfoData { get; }
        public List<GameObject> SpawnedMeshes { get; }
        public IChunk[] AdjacentChunks { get; }
        public bool AllCollapsed { get; }
        public int3 ChunkIndex { get; }
        public Cell[,,] Cells { get; }
        public bool IsRemoved { set; }
        public bool IsClear { get; }
        public Vector3 Position { get; }
        
        public int Width { get; }
        public int Height { get; }
        public int Depth { get; }
        

        public Cell this[int3 index]
        {
            get => Cells[index.x, index.y, index.z];
            set => Cells[index.x, index.y, index.z] = value;
        }

        public IChunk Construct(int _width, int _height, int _depth, int3 chunkIndex, Vector3 position, IChunk[] adjacentChunks, bool sideConstraints);
        public void Clear(Stack<GameObject> pool);
        public void SetAdjacentChunk(IChunk chunk, Direction oppositeDirection, Stack<ChunkIndex> cellStack);
        public void LoadCells(PrototypeInfoData prototypeInfoData, Vector3 gridScale, Stack<ChunkIndex> cellStack);
        
        public List<ChunkIndex> GetAdjacentCells(int3 index, out List<Direction> directions)
        {
            List<ChunkIndex> adjacentCells = new List<ChunkIndex>(6);
            directions = new List<Direction>(6);

            for (int i = 0; i < 6; i++)
            {
                int3 neighborIndex = index + ChunkWaveUtility.Directions[i];

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

                    int3 adjIndex = WrapIndexToAdjacentChunk(neighborIndex, i);
                    adjacentCells.Add(new ChunkIndex(AdjacentChunks[i].ChunkIndex, adjIndex));
                    directions.Add((Direction)i);
                }
            }

            return adjacentCells;
        }

        private bool IsWithinBounds(int3 index) =>
            index.x >= 0 && index.x < Width &&
            index.y >= 0 && index.y < Height &&
            index.z >= 0 && index.z < Depth;

        /// <summary>
        /// Gets the directions of the index that are invalid
        /// </summary>
        public List<Direction> GetInvalidAdjacentSides(int3 index, Stack<ChunkIndex> cellStack)
        {
            List<Direction> adjacentDirections = new List<Direction>();

            for (int i = 0; i < 6; i++)
            {
                int3 neighborIndex = index + ChunkWaveUtility.Directions[i];
                if (IsWithinBounds(neighborIndex)) continue;

                if (AdjacentChunks[i] == null || AdjacentChunks[i].PrototypeInfoData != PrototypeInfoData)
                {
                    adjacentDirections.Add((Direction)i);
                    continue;
                }

                if (AdjacentChunks[i].IsClear) continue;

                int3 adjacentIndex = WrapIndexToAdjacentChunk(index, i);
                Cell cell = AdjacentChunks[i][adjacentIndex];
                if (!cell.Collapsed) continue;

                if (cell.PossiblePrototypes[0].Keys[(int)WaveFunctionUtility.OppositeDirection(i)] == -1)
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

        private int3 WrapIndexToAdjacentChunk(int3 index, int direction)
        {
            return direction switch
            {
                0 => new int3(0, index.y, index.z), // Right
                1 => new int3(Width - 1, index.y, index.z), // Left
                2 => new int3(index.x, 0, index.z), // Up
                3 => new int3(index.x, Height - 1, index.z), // Down
                4 => new int3(index.x, index.y, 0), // Forward
                5 => new int3(index.x, index.y, Depth - 1), // Backward
                _ => throw new ArgumentOutOfRangeException(nameof(direction), "Invalid direction index")
            };
        }
    }

    public static class ChunkWaveUtility
    {
        public static readonly int3[] Directions =
        {
            new int3(1, 0, 0),
            new int3(-1, 0, 0),
            new int3(0, 1, 0),
            new int3(0, -1, 0),
            new int3(0, 0, 1),
            new int3(0, 0, -1),
        };

        public static int2 GetDistrictIndex2(Vector3 position, Vector3 chunkScale)
        {
            int x = Utility.Math.GetMultipleFloored(position.x, chunkScale.x);
            int y = Utility.Math.GetMultipleFloored(position.z, chunkScale.z);
            return new int2(x, y);
        }

        public static int3 GetDistrictIndex3(Vector3 position, Vector3 chunkScale)
        {
            int x = Utility.Math.GetMultipleFloored(position.x, chunkScale.x);
            int y = Utility.Math.GetMultipleFloored(position.y, chunkScale.y);
            int z = Utility.Math.GetMultipleFloored(position.z, chunkScale.z);
            return new int3(x, y, z);
        }

        public static Vector3 GetPosition(int3 index, Vector3 handlerChunkScale)
        {
            return handlerChunkScale.MultiplyByAxis(index);
        }
        
        public static List<ChunkIndex> GetNeighbouringChunkIndexes(ChunkIndex chunkIndex, int gridWidth, int gridHeight)
        {
            List<ChunkIndex> neighbours = new List<ChunkIndex>();
            for (int i = 0; i < WaveFunctionUtility.NeighbourDirections.Length; i++)
            {
                ChunkIndex neighbour = new ChunkIndex(chunkIndex.Index, chunkIndex.CellIndex + WaveFunctionUtility.NeighbourDirections[i].XyZ(0));
                if (neighbour.CellIndex.x < 0)
                {
                    neighbour.Index.x -= 1;
                    neighbour.CellIndex.x = gridWidth - 1;
                }
                else if (neighbour.CellIndex.x >= gridWidth)
                {
                    neighbour.Index.x += 1;
                    neighbour.CellIndex.x = 0;
                }
                
                if (neighbour.CellIndex.z < 0)
                {
                    neighbour.Index.z -= 1;
                    neighbour.CellIndex.z = gridHeight - 1;
                }
                else if (neighbour.CellIndex.z >= gridHeight)
                {
                    neighbour.Index.z += 1;
                    neighbour.CellIndex.z = 0;
                }
                neighbours.Add(neighbour);
            }
            return neighbours;
        }
        
        public static bool AreIndexesAdjacent(ChunkIndex chunkA, ChunkIndex chunkB, int chunkSize, out int3 diff)
        {
            int3 chunkAIndex = chunkA.Index * chunkSize + chunkA.CellIndex;
            int3 chunkBIndex = chunkB.Index * chunkSize + chunkB.CellIndex;
            diff = chunkBIndex - chunkAIndex;
            return math.cmax(math.abs(diff)) == 1 && math.csum(math.abs(diff)) == 1;
        }

        private static bool IsBorderCell(int chunkCoordA, int chunkCoordB, int cellCoordA, int cellCoordB, int chunkSize)
        {
            bool aOnBorder = (chunkCoordA < chunkCoordB) ? (cellCoordA == chunkSize - 1) : (cellCoordA == 0);
            bool bOnBorder = (chunkCoordA < chunkCoordB) ? (cellCoordB == 0) : (cellCoordB == chunkSize - 1);
            return aOnBorder && bOnBorder;
        }
    }

    public interface IChunkWaveFunction<TChunk> where TChunk : IChunk, new()
    {
        public ChunkWaveFunction<TChunk> ChunkWaveFunction { get; }
        public Vector3 ChunkScale { get; }
        public bool IsGenerating { get; }
    }
}



using Random = UnityEngine.Random;
using System.Collections.Generic;
using UnityEngine.Serialization;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;
using System;

namespace WaveFunctionCollapse
{
    
    [Serializable]
    public class ChunkWaveFunction
    {
        [Title("Grid Size")]
        [SerializeField]
        private Vector3 gridScale;

        [Title("Mesh")]
        [SerializeField]
        private MaterialData materialData;

        private readonly Stack<GameObject> gameObjectPool = new Stack<GameObject>();
        private readonly Stack<ChunkIndex> cellStack = new Stack<ChunkIndex>();

        private Transform parentTransform;
        private PrototypeData emptyPrototype;

        private IChunkWaveFunction handler;

        public Dictionary<int3, Chunk> Chunks { get; } = new Dictionary<int3, Chunk>();
        public Stack<GameObject> GameObjectPool => gameObjectPool;
        public Vector3 GridScale => gridScale;

        public Cell this[ChunkIndex index]
        {
            get => Chunks[index.Index][index.CellIndex];
            set => Chunks[index.Index][index.CellIndex] = value;
        }

        public bool Load(IChunkWaveFunction handler)
        {
            this.handler = handler;
            Clear();

            return true;
        }

        public Chunk LoadChunk(Vector3 position, Vector3Int size, PrototypeInfoData prototypeInfo)
        {
            int3 index = ChunkWaveUtility.GetDistrictIndex3(position, handler.ChunkScale);
            Chunk[] adjacentChunks = GetAdjacentChunks(index);
            Chunk chunk = new Chunk(size.x, size.y, size.z, index, position, adjacentChunks, gridScale);
            Chunks.Add(index, chunk);
            LoadCells(chunk, prototypeInfo);
            return chunk;
        }

        public void LoadCells(Chunk chunk, PrototypeInfoData prototypeInfo)
        {
            chunk.LoadCells(prototypeInfo, gridScale, cellStack);
            for (int i = 0; i < chunk.AdjacentChunks.Length; i++)
            {
                if (chunk.AdjacentChunks[i] == null || chunk.AdjacentChunks[i].IsClear || chunk.AdjacentChunks[i].AllCollapsed) continue;

                Direction oppositeDirection = WaveFunctionUtility.OppositeDirection(i);
                chunk.AdjacentChunks[i].SetAdjacentChunk(chunk, oppositeDirection, cellStack);
            }
        }

        private Chunk[] GetAdjacentChunks(int3 index)
        {
            Chunk[] adjacentChunks = new Chunk[6];

            for (int i = 0; i < 6; i++)
            {
                int3 adjacentIndex = index + ChunkWaveUtility.Directions[i];
                if (Chunks.TryGetValue(adjacentIndex, out Chunk chunk))
                {
                    adjacentChunks[i] = chunk;
                }
            }

            return adjacentChunks;
        }

        public void RemoveChunk(int3 chunkIndex, out List<Chunk> neighbours)
        {
            neighbours = new List<Chunk>();
            if (!Chunks.TryGetValue(chunkIndex, out Chunk chunk))
            {
                Debug.LogError("Chunk to remove not found");
                return;
            }

            for (int i = 0; i < chunk.AdjacentChunks.Length; i++)
            {
                if (chunk.AdjacentChunks[i] == null) continue;

                int oppositeDirection = (int)WaveFunctionUtility.OppositeDirection(i);
                chunk.AdjacentChunks[i].AdjacentChunks[oppositeDirection] = null;
                neighbours.Add(chunk.AdjacentChunks[i]);
            }

            chunk.Clear(gameObjectPool);
            chunk.IsRemoved = true;
            Chunks.Remove(chunkIndex);
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

            foreach (Chunk chunk in Chunks.Values)
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
                    index = new ChunkIndex(chunk.ChunkIndex, new int3(x, y, z));
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
            this[index] = new Cell(true, this[index].Position, new List<PrototypeData> { chosenPrototype });
            cellStack.Push(index);

            GameObject spawned = GenerateMesh(this[index].Position, chosenPrototype);
            if (spawned is not null)
            {
                Chunks[index.Index].SpawnedMeshes.Add(spawned);
            }
        }

        public void Propagate()
        {
            while (cellStack.TryPop(out ChunkIndex chunkIndex))
            {
                Cell changedCell = this[chunkIndex];
                List<ChunkIndex> neighbours = Chunks[chunkIndex.Index].GetAdjacentCells(chunkIndex.CellIndex, out List<Direction> directions);

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
            foreach (Chunk chunk in Chunks.Values)
            {
                chunk.Clear(gameObjectPool);
            }

            Chunks.Clear();
        }

        public bool AllCollapsed()
        {
            foreach (Chunk chunk in Chunks.Values)
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
            return Chunks.Count * 8;
        }
    }

    public readonly struct ChunkIndex
    {
        public readonly int3 Index;
        public readonly int3 CellIndex;

        public ChunkIndex(int3 index, int3 cellIndex)
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
        [FormerlySerializedAs("position")]
        [SerializeField, Sirenix.OdinInspector.ReadOnly]
        private Vector3 editorOnlyPosition;
#endif

        // In the Following order: Right, Left, Up, Down, Forward, Backward
        public readonly Chunk[] AdjacentChunks;
        public readonly Cell[,,] Cells;

        [NonSerialized]
        public List<GameObject> SpawnedMeshes = new List<GameObject>();

        public readonly Vector3 Position;

        public readonly int width;
        public readonly int height;
        public readonly int depth;

        public int3 ChunkIndex { get; set; }
        public bool IsRemoved { get; set; }
        public bool IsClear { get; private set; } = true;
        public PrototypeInfoData PrototypeInfoData { get; set; }

        public Cell this[int3 index]
        {
            get => Cells[index.x, index.y, index.z];
            set => Cells[index.x, index.y, index.z] = value;
        }

        public Chunk(int _width, int _height, int _depth, int3 chunkIndex, Vector3 position, Chunk[] adjacentChunks, Vector3 gridScale)
        {
            Position = position;
            AdjacentChunks = adjacentChunks;
            width = _width;
            height = _height;
            depth = _depth;
            ChunkIndex = chunkIndex;

            Cells = new Cell[width, height, depth];

#if UNITY_EDITOR
            editorOnlyPosition = position;
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

        public void LoadCells(PrototypeInfoData prototypeInfoData, Vector3 gridScale, Stack<ChunkIndex> cellStack)
        {
            PrototypeInfoData = prototypeInfoData;
            IsClear = false;

            for (int z = 0; z < depth; z++)
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int3 index = new int3(x, y, z);
                Vector3 pos = new Vector3(x * gridScale.x, y * gridScale.y, z * gridScale.z);

                bool isBottom = AdjacentChunks[3] == null && y == 0;
                List<PrototypeData> prots = new List<PrototypeData>(isBottom ? prototypeInfoData.Prototypes : prototypeInfoData.NotBottomPrototypes);
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

                Cells[x, y, z] = new Cell(false, pos + Position, prots);
                if (changed)
                {
                    cellStack.Push(new ChunkIndex(ChunkIndex, index));
                }
            }
        }

        public List<ChunkIndex> GetAdjacentCells(int3 index, out List<Direction> directions)
        {
            List<ChunkIndex> adjacentCells = new List<ChunkIndex>();
            directions = new List<Direction>();

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
            index.x >= 0 && index.x < width &&
            index.y >= 0 && index.y < height &&
            index.z >= 0 && index.z < depth;

        /// <summary>
        /// Gets the directions of the index that are invalid
        /// </summary>
        public List<Direction> GetAdjacentSides(int3 index, Stack<ChunkIndex> cellStack)
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

        private int3 WrapIndexToAdjacentChunk(int3 index, int direction)
        {
            return direction switch
            {
                0 => new int3(0, index.y, index.z), // Right
                1 => new int3(width - 1, index.y, index.z), // Left
                2 => new int3(index.x, 0, index.z), // Up
                3 => new int3(index.x, height - 1, index.z), // Down
                4 => new int3(index.x, index.y, 0), // Forward
                5 => new int3(index.x, index.y, depth - 1), // Backward
                _ => throw new ArgumentOutOfRangeException(nameof(direction), "Invalid direction index")
            };
        }

        public void SetAdjacentChunk(Chunk chunk, Direction direction, Stack<ChunkIndex> cellStack)
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
                int3 index = new int3(x, y, z);
                bool isBottom = AdjacentChunks[3] == null && y == 0;
                List<PrototypeData> prots = new List<PrototypeData>(isBottom ? PrototypeInfoData.Prototypes : PrototypeInfoData.NotBottomPrototypes);
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
            int x = Math.GetMultipleFloored(position.x, chunkScale.x);
            int y = Math.GetMultipleFloored(position.z, chunkScale.z);
            return new int2(x, y);
        }

        public static int3 GetDistrictIndex3(Vector3 position, Vector3 chunkScale)
        {
            int x = Math.GetMultipleFloored(position.x, chunkScale.x);
            int y = Math.GetMultipleFloored(position.y, chunkScale.y);
            int z = Math.GetMultipleFloored(position.z, chunkScale.z);
            return new int3(x, y, z);
        }
    }

    public interface IChunkWaveFunction
    {
        public ChunkWaveFunction ChunkWaveFunction { get; }
        public Vector3 ChunkScale { get; }
    }
}



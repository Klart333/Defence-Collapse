using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace WaveFunctionCollapse
{
    public class QueryMarchedChunk : IChunk
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

        public Vector3 Position { get; private set;}
        public int Width { get; private set;}
        public int Height { get; private set;}
        public int Depth { get; private set;}
        public int3 ChunkIndex { get; set; }
        public bool IsRemoved { get; set; }
        public bool UseSideConstraints { get; private set; }
        public bool IsClear { get; private set; } = true;
        public PrototypeInfoData PrototypeInfoData { get; set; }
        
        public Vector3Int ChunkSize => new Vector3Int(Width, Height, Depth);
        
        public Cell this[int3 index]
        {
            get => Cells[index.x, index.y, index.z];
            set => Cells[index.x, index.y, index.z] = value;
        }

        public IChunk Construct(int _width, int _height, int _depth, int3 chunkIndex, Vector3 position, IChunk[] adjacentChunks, Vector3 gridScale, bool sideConstraints)
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

                Cells[x, y, z] = new Cell(false, pos + Position, prots);
            }

            void ConstrainBySides(int3 index, List<PrototypeData> prots)
            {
                List<Direction> directions = GetInvalidAdjacentSides(index, cellStack);
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
                List<Direction> directions = GetInvalidAdjacentSides(index, cellStack);
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

            for (int i = SpawnedMeshes.Count - 1; i >= 0; i--)
            {
                SpawnedMeshes[i].SetActive(false);
                pool.Push(SpawnedMeshes[i]);
                SpawnedMeshes.RemoveAt(i);
            }
            
            OnCleared?.Invoke();
        }
    }
}
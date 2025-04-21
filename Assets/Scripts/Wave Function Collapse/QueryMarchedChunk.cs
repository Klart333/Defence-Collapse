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
                
        private PrototypeData unbuildablePrototype;
        private List<PrototypeData> unbuildablePrototypeList;
        
        public readonly List<(int3, Cell)> QueryChangedCells = new List<(int3, Cell)>();
        public readonly List<int3> QueryCollapsedAir = new List<int3>();
        
        private int3 queryIndex;
        private bool shouldRemoveQueryIndex;

        // In the Following order: Right, Left, Up, Down, Forward, Backward
        public IChunk[] AdjacentChunks { get; private set; }
        public IQueryWaveFunction Handler { get; set; }
        public Cell[,,] Cells { get; private set; }
        public bool[,,] BuiltCells { get; private set; }
        public List<GameObject> SpawnedMeshes { get; } = new List<GameObject>();
        public Vector3 Position { get; private set;}
        public int Width { get; private set;}
        public int Height { get; private set;}
        public int Depth { get; private set;}
        public int3 ChunkIndex { get; set; }
        public bool IsRemoved { get; set; }
        public bool UseSideConstraints { get; private set; }
        public bool IsClear { get; private set; } = true;
        public PrototypeInfoData PrototypeInfoData { get; private set; }
        
        public int3 ChunkSize => new int3(Width, Height, Depth);

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
            BuiltCells = new bool[Width, Height, Depth];
            
            unbuildablePrototype = new PrototypeData(new MeshWithRotation(-1, 0), -1, -1, -1, -1, -1, -1, 0, Array.Empty<int>());
            unbuildablePrototypeList = new List<PrototypeData> { unbuildablePrototype };
            
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
            throw new System.NotImplementedException();
        }
        
        public void LoadCells(PrototypeInfoData prototypeInfoData, Vector3 gridScale, Chunk groundChunk, Vector3 offset, BuildableCornerData cellBuildableCornerData = null)
        {
            PrototypeInfoData = prototypeInfoData;
            IsClear = false;

            for (int z = 0; z < Depth; z++)
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                Vector3 pos = new Vector3(x * gridScale.x, y * gridScale.y, z * gridScale.z) - offset;
                Cells[x, y, z] = new Cell(false, Position + pos, new List<PrototypeData> { PrototypeData.Empty });
            }
            
            for (int z = 0; z < Depth; z++)
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                int3 cellIndex = new int3(x, y, z);
                int3 gridIndex = new int3(Mathf.FloorToInt(x * gridScale.x / 2.0f), 0, Mathf.FloorToInt(z * gridScale.z / 2.0f));
                SetCellDependingOnGround(cellIndex, gridIndex); 
            }
            
            void SetCellDependingOnGround(int3 cellIndex, int3 groundIndex)
            {
                Vector3 cellPosition = Cells[cellIndex.x, cellIndex.y, cellIndex.z].Position;

                if (!groundChunk.Cells.IsInBounds(groundIndex.x, 0, groundIndex.z))
                {
                    Debug.LogError("Not in bounds: " + groundIndex + ", cell: " + cellIndex);
                    return;
                }
                Cell groundCell = groundChunk.Cells[groundIndex.x, 0, groundIndex.z];
                Vector2Int corner = new Vector2Int((int)Mathf.Sign(groundCell.Position.x - cellPosition.x), (int)Mathf.Sign(groundCell.Position.z - cellPosition.z));

                if (cellBuildableCornerData != null && !cellBuildableCornerData.IsCornerBuildable(groundCell.PossiblePrototypes[0].MeshRot, corner, out _))
                {
                    Cells[cellIndex.x, cellIndex.y, cellIndex.z] = new Cell(
                        true,
                        cellPosition,
                        unbuildablePrototypeList,
                        false);
                }
            }
        }

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

        /// <remarks>Does not use height</remarks>>
        public bool ContainsPoint(Vector3 point, Vector3 scale)
        {
            Vector3 min = Position - scale / 2.0f;
            return point.x < min.x + Width * scale.x && point.x >= min.x 
                && point.z < min.z + Depth * scale.z && point.z >= min.z;
        }
        
        #region Query

        public void Place()
        {
            UncollapseAir();

            shouldRemoveQueryIndex = false;
            QueryChangedCells.Clear();
        }
        
        private void UncollapseAir()
        {
            for (int i = 0; i < QueryCollapsedAir.Count; i++)
            {
                int3 index = QueryCollapsedAir[i];
                bool fixedit = false;
                for (int g = 0; g < QueryChangedCells.Count; g++)
                {
                    if (!index.Equals(QueryChangedCells[g].Item1)) continue;
                
                    fixedit = true;
                    int3 changedIndex = QueryChangedCells[g].Item1;
                    if (QueryChangedCells[g].Item2.Collapsed)
                    {
                        Handler.SetCell(new ChunkIndex(ChunkIndex,  changedIndex), QueryChangedCells[g].Item2.PossiblePrototypes[0], QueryCollapsedAir, false);
                    }
                    else
                    {
                        this[changedIndex] = QueryChangedCells[g].Item2;
                    }
                    break;
                }
            
                if (!fixedit)
                {
                    this[index] = new Cell(false, this[index].Position, new List<PrototypeData> { PrototypeData.Empty });
                }
            }
            QueryCollapsedAir.Clear();
        }
        
        public void RevertQuery()
        {
            for (int i = 0; i < QueryChangedCells.Count; i++)
            {
                int3 index = QueryChangedCells[i].Item1;
                if (QueryChangedCells[i].Item2.Collapsed)
                {
                    Handler.SetCell(new ChunkIndex(ChunkIndex, index), QueryChangedCells[i].Item2.PossiblePrototypes[0], QueryCollapsedAir, false);
                }
                else
                {
                    this[index] = QueryChangedCells[i].Item2;
                }
            }

            if (shouldRemoveQueryIndex)
            {
                shouldRemoveQueryIndex = false;
                BuiltCells[queryIndex.x, queryIndex.y, queryIndex.z] = false;
            }
        
            QueryChangedCells.Clear();
            QueryCollapsedAir.Clear();
        }
        
        public void SetBuiltCells(int3 queryIndex)
        {
            this.queryIndex = queryIndex;
            
            if (BuiltCells[queryIndex.x, queryIndex.y, queryIndex.z])
            {
                shouldRemoveQueryIndex = false;
            }
            else
            {
                shouldRemoveQueryIndex = true;
                BuiltCells[queryIndex.x, queryIndex.y, queryIndex.z] = true;
            }
        }
        
        #endregion
        
    }
}
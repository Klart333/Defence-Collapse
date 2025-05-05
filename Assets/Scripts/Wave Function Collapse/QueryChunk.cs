using System.Collections.Generic;
using UnityEngine.Serialization;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using System;

namespace WaveFunctionCollapse
{
    public class QueryChunk : IChunk
    {
#if UNITY_EDITOR
        [FormerlySerializedAs("position")]
        [SerializeField, Sirenix.OdinInspector.ReadOnly]
        private Vector3 editorOnlyPosition;
#endif
        
        public event Action OnCleared;
                
        private PrototypeData unbuildablePrototype;
        private List<PrototypeData> unbuildablePrototypeList;
        
        public readonly List<Tuple<int3, Cell>> QueryChangedCells = new List<Tuple<int3, Cell>>();
        public List<GameObject> SpawnedMeshes => throw new NotImplementedException();

        public bool IsChunkQueryAdded { get; private set; } = true;
        
        // In the Following order: Right, Left, Up, Down, Forward, Backward
        public IChunk[] AdjacentChunks { get; private set; }
        public Cell[,,] Cells { get; private set; }
        public bool[,,] BuiltCells { get; private set; }
        public Vector3 Position { get; private set;}
        public int Width { get; private set;}
        public int Height { get; private set;}
        public int Depth { get; private set;}
        public int3 ChunkIndex { get; set; }
        public bool IsRemoved { get; set; }
        public bool UseSideConstraints { get; private set; }
        public bool IsClear { get; private set; } = true;
        public PrototypeInfoData PrototypeInfoData { get; set; }
        
        public int3 ChunkSize => new int3(Width, Height, Depth);
        
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

                Cell cell = new Cell(false, pos + Position, prots);
                Cells[x, y, z] = cell;
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
            IsChunkQueryAdded = false;
            QueryChangedCells.Clear();
        }
        
        public void RevertQuery(Action<ChunkIndex, PrototypeData, bool> setCell, Action<int3> removeChunk)
        {
            if (IsChunkQueryAdded)
            {
                removeChunk?.Invoke(ChunkIndex);
                return;
            }
            
            for (int i = 0; i < QueryChangedCells.Count; i++)
            {
                int3 index = QueryChangedCells[i].Item1;
                if (QueryChangedCells[i].Item2.Collapsed)
                {
                    Debug.Log("Resetting cell to: " + QueryChangedCells[i].Item2.PossiblePrototypes[0].Name_EditorOnly);
                    setCell.Invoke(new ChunkIndex(ChunkIndex, index), QueryChangedCells[i].Item2.PossiblePrototypes[0], false);
                }
                else
                {
                    this[index] = QueryChangedCells[i].Item2;
                }
            }

            QueryChangedCells.Clear();
        }
        
        public void QueryClear()
        {
            for (int z = 0; z < Depth; z++)
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                Cell cell = Cells[x, y, z];
                QueryChangedCells.Add(Tuple.Create(new int3(x, y, z), cell));
                
                bool isBottom = AdjacentChunks[3] == null && y == 0;
                List<PrototypeData> prots = new List<PrototypeData>(isBottom ? PrototypeInfoData.Prototypes : PrototypeInfoData.NotBottomPrototypes);
                cell.PossiblePrototypes = new List<PrototypeData>( prots);
                cell.Collapsed = false;
                Cells[x, y, z] = cell;
            }
            
            OnCleared?.Invoke();
        }
        
        public void QueryLoad(PrototypeInfoData prototypeInfoData, Vector3 gridScale, Stack<ChunkIndex> cellStack)
        {
            for (int z = 0; z < Depth; z++)
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                QueryChangedCells.Add(Tuple.Create(new int3(x, y, z), Cells[x, y, z]));
            }
            
            LoadCells(prototypeInfoData, gridScale, cellStack);
        }
        
        #endregion
    }
}
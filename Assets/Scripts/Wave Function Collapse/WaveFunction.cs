using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using System.Collections.Generic;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Unity.Collections;
using System.Text;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System;
using System.Collections.Specialized;
using Unity.Mathematics;
using UnityEngine.Serialization;

namespace WaveFunctionCollapse
{

    [Serializable]
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
        private PrototypeInfoData prototypeInfo;
        
        [SerializeField]
        private ProtoypeMeshes prototypeMeshes;

        private readonly List<PrototypeData> bottomPrototypes = new List<PrototypeData>();
        private readonly List<GameObject> spawnedPossibilites = new List<GameObject>();
        private readonly List<GameObject> spawnedMeshes = new List<GameObject>();
        private List<PrototypeData> prototypes = new List<PrototypeData>();
        private readonly Stack<int> cellStack = new Stack<int>();
        private readonly List<Cell> cells = new List<Cell>();
        
        public Vector3Int GridSize => new Vector3Int(gridSizeX, gridSizeY, gridSizeZ);
        public bool AllCollapsed => cells.All(cell => cell.Collapsed);
        public List<Cell> Cells => cells;
        public Vector3 GridScale => gridScale;

        public Transform ParentTransform { get; set; }
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

        public Cell Iterate()
        {
            int index = GetLowestEntropyIndex();

            PrototypeData chosenPrototype = Collapse(cells[index]);
            SetCell(index, chosenPrototype);

            Propagate();

            return cells[index];
        }

        public int GetLowestEntropyIndex()
        {
            float lowestEntropy = 10000;
            int index = 0;

            for (int i = 0; i < cells.Count; i++)
            {
                Cell cell = cells[i];
                if (cell.Collapsed)
                {
                    continue;
                }

                float cellEntropy = GetCords(i).y * 100; // Add the Y level
                if (cellEntropy > lowestEntropy)
                {
                    continue;
                }

                cellEntropy += cell.Entropy;
                cells[i] = cell;
                
                if (cellEntropy < lowestEntropy)
                {
                    lowestEntropy = cellEntropy;
                    index = i;
                }
            }

            return index;
        }

        public PrototypeData Collapse(Cell cell)
        {
            if (cell.PossiblePrototypes.Count == 0)
            {
                return PrototypeData.Empty;
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
                return null;
            }

            // Check downward
            // Rule is above flat tile is only air
            int directIndex = index;
            bool onlyAir = true;
            bool noConnectionsUp = true;

            while (IsDirectionValid(directIndex, Direction.Down, out directIndex) && onlyAir)
            {
                bool hasConnections = false;
                foreach (PrototypeData proto in cells[directIndex].PossiblePrototypes)
                {
                    if (!hasConnections && proto.PosY != 1)
                    {
                        hasConnections = true;
                    }

                    if ((proto.NegY | proto.PosX | proto.NegX | proto.NegZ | proto.PosZ) > 1)
                    {
                        onlyAir = false;
                    }
                }

                noConnectionsUp = !hasConnections;
            }

            if (!onlyAir && noConnectionsUp)
            {
                SetCell(index, PrototypeData.Empty);
                changed = false;
                return null;
            }

            // Check Directly Above
            // Rule is Below flat tile is only air
            if (IsDirectionValid(index, Direction.Up, out int aboveIndex)
                && cells[aboveIndex].Collapsed
                && cells[aboveIndex].PossiblePrototypes[0].NegY == 1)
            {
                PrototypeData proto = cells[aboveIndex].PossiblePrototypes[0];
                bool airAbove = (proto.PosY | proto.PosX | proto.NegX | proto.NegZ | proto.PosZ) <= 1;

                if (!airAbove)
                {
                    SetCell(index, PrototypeData.Empty);
                    changed = false;
                    return null;
                }
            }

            WaveFunctionUtility.Constrain(changedCell, affectedCell, direction, out changed);

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
            for (int z = 0; z < gridSizeZ; z++)
            for (int y = 0; y < gridSizeY; y++)
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
                        bool shouldRemove = prots[i].DirectionToKey(direction) != 1;

                        if (shouldRemove) prots.RemoveAt(i);
                    }
                }

                cells.Add(new Cell(false, pos + OriginPosition, prots));
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
            if (prototypeData.MeshRot.MeshIndex == -1)
            {
                return null;
            }

            GameObject gm = new GameObject();
            gm.AddComponent<MeshFilter>().mesh = prototypeMeshes.Meshes[prototypeData.MeshRot.MeshIndex];
            gm.AddComponent<MeshRenderer>().SetMaterials(materialData.GetMaterials(prototypeData.MaterialIndexes));

            gm.transform.position = position;
            gm.transform.rotation = Quaternion.Euler(0, 90 * prototypeData.MeshRot.Rot, 0);
            ParentTransform ??= new GameObject("Wave Function Parent").transform;
            gm.transform.SetParent(ParentTransform, true);

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

#if UNITY_EDITOR
        public void DrawGizmos()
        {
            if (!EditorApplication.isPlaying)
            {
                return;
            }

            foreach (var cell in cells)
            {
                Vector3 pos = cell.Position;
                Gizmos.color = Color.white;
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
                    if (cells[i].PossiblePrototypes[g].MeshRot.MeshIndex == -1)
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
#endif
        #endregion
    }

    [Serializable]
    public struct MeshWithRotation : IEquatable<MeshWithRotation>
    {
        [FormerlySerializedAs("Mesh")]
        public int MeshIndex;
        public int Rot;

        public MeshWithRotation(int meshIndex, int rot)
        {
            MeshIndex = meshIndex;
            Rot = rot;
        }

        public bool Equals(MeshWithRotation other)
        {
            return Equals(MeshIndex, other.MeshIndex) && Rot == other.Rot;
        }

        public override bool Equals(object obj)
        {
            return obj is MeshWithRotation other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(MeshIndex, Rot);
        }
    }

    [Serializable]
    public struct Cell : IEquatable<Cell>
    {
        public bool Collapsed;

        public Vector3 Position;

        public List<PrototypeData> PossiblePrototypes; // SHOULD BE AN ARRAY 

        private float entropy;
        private bool isEntropyDirty;
        
        public float Entropy
        {
            get
            {
                if (isEntropyDirty)
                {
                    entropy = WaveFunctionUtility.CalculateEntropy(this);
                    isEntropyDirty = false;
                }

                return entropy;
            }
        }

        public Cell(bool collapsed, Vector3 position, List<PrototypeData> possiblePrototypes)
        {
            Collapsed = collapsed;
            Position = position;
            PossiblePrototypes = possiblePrototypes;
            
            isEntropyDirty = true;
            entropy = 0.0f;
        }

        public void SetDirty()
        {
            isEntropyDirty = true;
        }

        public override bool Equals([CanBeNull] object obj)
        {
            if (obj is Cell cell)
                return Equals(cell);

            return false;
        }

        public bool Equals(Cell other)
        {
            return other.Position == Position;
        }

        public override int GetHashCode()
        {
            return Position.GetHashCode();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"Cell(Position: {Position}, Collapsed: {Collapsed}, PossiblePrototypes: [");

            if (PossiblePrototypes != null)
            {
                for (int i = 0; i < PossiblePrototypes.Count; i++)
                {
                    sb.Append(PossiblePrototypes[i].ToString());
                    if (i < PossiblePrototypes.Count - 1)
                        sb.Append(", ");
                }
            }

            sb.Append("])");
            return sb.ToString();
        }
    }
    
    public static class WaveFunctionUtility
    {
        public static readonly int2[] NeighbourDirections =
        {
            new int2(1, 0),
            new int2(0, 1),
            new int2(-1, 0),
            new int2(0, -1),
        };
        
        public static readonly int2[] DiagonalNeighbourDirections = 
        {
            new int2(1, 0),
            new int2(0, 1),
            new int2(-1, 0),
            new int2(0, -1),
            new int2(1, 1),
            new int2(1, -1),
            new int2(-1, 1),
            new int2(-1, -1),
        };
        
        public static readonly List<int2> MarchDirections = new List<int2>
        {
            new int2(-1, -1),
            new int2(0, -1),
            new int2(0, 0), 
            new int2(-1, 0),
        };
        
        public static readonly int2[] Corners =
        {
            new int2(-1, -1),
            new int2(1, -1),
            new int2(1, 1),
            new int2(-1, 1),
        };
        
        public const ulong SymmetricalMask = 0x0000_0000_FFFF_FFFF;
        public const ulong NotFMask        = 0x0000_FFFF_0000_0000;
        public const ulong FMask           = 0xFFFF_0000_0000_0000;
        
        public static float CalculateEntropy(Cell cell)
        {
            float totalWeight = 0;
            float entropy = 0;
            for (int i = 0; i < cell.PossiblePrototypes.Count; i++)
            {
                totalWeight += cell.PossiblePrototypes[i].Weight;
            }

            for (int i = 0; i < cell.PossiblePrototypes.Count; i++)
            {
                float probability = cell.PossiblePrototypes[i].Weight / totalWeight;
                entropy += probability * math.log(probability);
            }

            return -entropy;
        }
        
        public static void Constrain(Cell changedCell, Cell affectedCell, Direction direction, out bool changed)
        {
            changed = false;
            if (affectedCell.Collapsed) return;

            ulong validKeys = 0;
            foreach (PrototypeData prot in changedCell.PossiblePrototypes)
            {
                ulong mask = prot.DirectionToKey(direction);
                validKeys |= mask;
            }

            Direction oppositeDirection = OppositeDirection(direction);
            for (int i = affectedCell.PossiblePrototypes.Count - 1; i >= 0; i--)
            {
                if (CheckValidSocket(affectedCell.PossiblePrototypes[i].DirectionToKey(oppositeDirection), validKeys)) continue;

                affectedCell.PossiblePrototypes.RemoveAtSwapBack(i);
                changed = true;
            }

            if (affectedCell.PossiblePrototypes.Count == 0)
            {
                affectedCell.PossiblePrototypes.Add(PrototypeData.Empty);
            }

            if (changed)
            {
                affectedCell.SetDirty();
            }
        }

        public static bool CheckValidSocket(ulong key, ulong validKeys)
        {
            return (key & SymmetricalMask & validKeys) > 0
                   || ((key & FMask) >> 16 & validKeys) > 0
                   || ((key & NotFMask) << 16 & validKeys) > 0;
        }
        
        public static Direction OppositeDirection(Direction direction)
        {
            int intDirection = (int)direction;
            return (Direction)(intDirection + 1 - intDirection % 2 * 2);
            
            //return direction switch
            //{
            //    Direction.Right => Direction.Left,
            //    Direction.Left => Direction.Right,
            //    Direction.Up => Direction.Down,
            //    Direction.Down => Direction.Up,
            //    Direction.Forward => Direction.Backward,
            //    Direction.Backward => Direction.Forward,
            //};
        }

        public static Direction OppositeDirection(int direction)
        {
            return (Direction)(direction + 1 - direction % 2 * 2);
        }
    }
}
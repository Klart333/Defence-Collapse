using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Unity.Jobs;

public class PathManager : Singleton<PathManager>
{
    [Title("Flood fill")]
    [InfoBox("Max size 65536 (256x256)")]
    [SerializeField]
    private Vector2Int gridSize;

    [SerializeField]
    private float cellScale;

    [Title("Portal")]
    [SerializeField]
    private GroundObjectData portalObjectData;

    private NativeArray<PathCell> cells;
    private NativeArray<float2> directions;
    private NativeArray<int> distances;
    private NativeArray<int2> neighbourDirections;

    private readonly HashSet<Blocker> blockers = new HashSet<Blocker>();
    private readonly HashSet<int> blockedIndexes = new HashSet<int>();
    private readonly List<Portal> portals = new List<Portal>();

    private JobHandle jobHandle;

    public NativeArray<float2> Directions => directions;
    public float CellScale => cellScale;
    public int GridHeight => gridSize.y;
    public int GridWidth => gridSize.x;
    public float GridWorldWidth => GridWidth * CellScale;
    public float GridWorldHeight => GridHeight * CellScale;

    private void OnEnable()
    {
        portalObjectData.OnObjectSpawned += OnPortalPlaced;

        int length = gridSize.x * gridSize.y;
        cells = new NativeArray<PathCell>(length, Allocator.Persistent);
        directions = new NativeArray<float2>(length, Allocator.Persistent);
        distances = new NativeArray<int>(length, Allocator.Persistent);
        neighbourDirections = new NativeArray<int2>(new int2[] { new int2(1, 0), new int2(1, 1), new int2(0, 1), new int2(-1, 1), new int2(-1, 0), new int2(-1, -1), new int2(0, -1), new int2(1, -1), }, Allocator.Persistent);
    }

    private void OnDisable()
    {
        portalObjectData.OnObjectSpawned -= OnPortalPlaced;

        cells.Dispose();
        directions.Dispose();
        distances.Dispose(); 
        neighbourDirections.Dispose();
    }

    private void Update()
    {
        UpdateFloodFill();
    }

    private void OnPortalPlaced(GameObject spawnedObject) 
    {
        if (spawnedObject.TryGetComponent(out Portal portal))
        {
            portals.Add(portal);
        }
        else
        {
            Debug.LogError("Could not find portal script on spawned portal?");
        }
    }

    public List<Vector3> GetEnemySpawnPoints()
    {
        List<Vector3> spawnPoints = new List<Vector3>();
        for (int i = 0; i < portals.Count; i++)
        {
            if (portals[i].Locked)
            {
                continue;
            }

            Vector3 portalPos = portals[i].transform.position;
            spawnPoints.Add(portalPos);
        }

        return spawnPoints;
    }

    private void UpdateFloodFill()
    {
        for (int i = 0; i < cells.Length; i++)
        {
            bool walkable = !blockedIndexes.Contains(i);

            cells[i] = new PathCell
            {
                Index = (ushort)i,
                MovementCost = 1,
                IsTarget = i % 166 == 0,
                IsWalkable = walkable,
            };
        }

        DistanceJob distanceJob = new DistanceJob()
        {
            cells = cells,
            distances = distances,
            neighbourDirections = neighbourDirections,
            GridWidth = gridSize.x,
        };

        jobHandle = distanceJob.Schedule();
        jobHandle.Complete();

        PathJob pathJob = new PathJob()
        {
            directions = directions,
            distances = distances,
            neighbourDirections = neighbourDirections,
            GridWidth = gridSize.x,
        };
        jobHandle = pathJob.Schedule(gridSize.y, 32);
        jobHandle.Complete();
    }

    public void RegisterBlocker(Blocker blocker)
    {
        if (!blockers.Add(blocker))
        {
            Debug.LogError("Trying to add same blocker again");
            return;
        }
        Debug.Log("Added Blocker");
        blocker.OnBlockerRebuilt += RebuildBLockerHashSet;
    }

    public void UnregisterBlocker(Blocker blocker)
    {
        if (!blockers.Remove(blocker))
        {
            Debug.LogError("Trying to remove non-registered blocker");
        }

        blocker.OnBlockerRebuilt -= RebuildBLockerHashSet;
    }

    private void RebuildBLockerHashSet()
    {
        blockedIndexes.Clear();
        foreach (var blocker in blockers)
        {
            for (int i = 0; i < blocker.BlockedIndexes.Count; i++)
            {
                blockedIndexes.Add(blocker.BlockedIndexes[i]);
            }
        }
    }

    #region Debug

    private void OnDrawGizmosSelected()
    {
        if (directions == null || directions.Length <= 0)
        {
            return;
        }

        Gizmos.color = Color.red;
        for (int i = 0; i < directions.Length; i++)
        {
            Vector3 pos = new Vector3(i % gridSize.x, 5, i / gridSize.x) * cellScale;
            Gizmos.DrawLine(pos, pos + new Vector3(directions[i].x, 0, directions[i].y) * cellScale);
        }
    }

    #endregion

    #region Utility

    public bool CheckIfValid(float xPos, float zPos)
    {
        return xPos > 0 && zPos > 0 && xPos < GridWorldWidth && zPos < GridWorldHeight;
    }

    public bool CheckIfValid(Vector2 pos)
    {
        return pos.x > 0 && pos.y > 0 && pos.x < GridWorldWidth && pos.y < GridWorldHeight;
    }

    public int GetIndex(float xPos, float zPos)
    {
        return Math.GetMultiple(xPos, CellScale) + Math.GetMultiple(zPos, CellScale) * GridWidth;
    }

    public int GetIndex(Vector2 pos)
    {
        return Math.GetMultiple(pos.x, CellScale) + Math.GetMultiple(pos.y, CellScale) * GridWidth;
    }

    public Vector2 GetPos(int index)
    {
        return new Vector2(index % GridWidth, Mathf.FloorToInt(index / GridWidth)) * CellScale;
    }

    #endregion
}

public struct PathCell
{
    public bool IsWalkable;
    public bool IsTarget;
    public byte MovementCost;
    public ushort Index;
}

public struct DistanceJob : IJob
{
    public NativeArray<int> distances;

    [Unity.Collections.ReadOnly]
    public NativeArray<PathCell> cells;

    [Unity.Collections.ReadOnly]
    public int GridWidth;

    [Unity.Collections.ReadOnly]
    public NativeArray<int2> neighbourDirections;

    public void Execute()
    {
        NativeQueue<PathCell> frontierQueue = new NativeQueue<PathCell>(Allocator.Temp);
        NativeHashSet<int> visited = new NativeHashSet<int>(10, Allocator.Temp);

        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i].IsTarget)
            {
                frontierQueue.Enqueue(cells[i]);
                visited.Add(i);
            }
        }

        NativeArray<int> neighbours = new NativeArray<int>(8, Allocator.Temp);
        while (frontierQueue.TryDequeue(out PathCell cell))
        {
            int count = GetNeighbours(cell.Index, visited, neighbours);
            for (int i = 0; i < count; i++)
            {
                PathCell neighbour = cells[neighbours[i]];
                visited.Add(neighbour.Index);

                if (!neighbour.IsWalkable)
                {
                    distances[neighbour.Index] = 1000;
                    continue;
                }

                distances[neighbour.Index] = distances[cell.Index] + neighbour.MovementCost;
                frontierQueue.Enqueue(neighbour);
            }
        }

        frontierQueue.Dispose();
        visited.Dispose();
        neighbours.Dispose();
    }

    private int GetNeighbours(int index, NativeHashSet<int> set, NativeArray<int> array)
    {
        int count = 0;
        for (int i = 0; i < neighbourDirections.Length; i++)
        {
            int n = index + neighbourDirections[i].x + neighbourDirections[i].y * GridWidth;
            if (n >= 0 && n < cells.Length && !set.Contains(n))
            {
                array[count++] = n;
            }
        }

        return count;
    }
}

public struct PathJob : IJobParallelFor
{
    [NativeDisableParallelForRestriction]
    public NativeArray<float2> directions;

    [Unity.Collections.ReadOnly]
    public NativeArray<int> distances;

    [Unity.Collections.ReadOnly]
    public NativeArray<int2> neighbourDirections;

    [Unity.Collections.ReadOnly]
    public int GridWidth;

    public void Execute(int index)
    {
        for (int i = 0; i < GridWidth; i++)
        {
            int cellIndex = index * GridWidth + i;
            int lowestCost = int.MaxValue;
            int2 lowestCostDir = 0;
            for (int j = 0; j < neighbourDirections.Length; j++)
            {
                int neighbour = cellIndex + neighbourDirections[j].x + neighbourDirections[j].y * GridWidth;
                if (neighbour >= 0 && neighbour < distances.Length && distances[neighbour] < lowestCost)
                {
                    lowestCost = distances[neighbour];
                    lowestCostDir = neighbourDirections[j];
                }
            }

            directions[cellIndex] = lowestCostDir;
        }
    }
}
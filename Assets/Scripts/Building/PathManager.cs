using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Unity.Jobs;

public class PathManager : Singleton<PathManager>
{
    [Title("Flood fill")]
    [SerializeField]
    private Vector2Int gridResolution;

    [Title("Portal")]
    [SerializeField]
    private GroundObjectData portalObjectData;

    private NativeArray<PathCell> cells;
    private NativeArray<float2> directions;
    private NativeArray<int> distances;
    private NativeArray<int2> neighbourDirections;

    private readonly HashSet<Vector3Int> buildingPositions = new HashSet<Vector3Int>();
    private readonly HashSet<Vector3Int> blacklistedBuildingPositions = new HashSet<Vector3Int>();

    private readonly List<Portal> portals = new List<Portal>();

    private JobHandle jobHandle;
    private BuildingHandler buildingHandler;
    private WaveFunction waveFunction;

    private Vector3Int castleIndex;

    private bool updatingFloodFill;
    private bool updateQueued;

    public NativeArray<float2> Directions => directions;

    private void OnEnable()
    {
        buildingHandler = FindAnyObjectByType<BuildingHandler>();
        waveFunction = FindAnyObjectByType<WaveFunction>();

        Events.OnBuildingDestroyed += BuildingHandler_OnBuildingDestroyed;
        Events.OnBuildingRepaired += BuildingHandler_OnBuildingRepaired;
        portalObjectData.OnObjectSpawned += OnPortalPlaced;

        int length = gridResolution.x * gridResolution.y;
        cells = new NativeArray<PathCell>(length, Allocator.Persistent);
        directions = new NativeArray<float2>(length, Allocator.Persistent);
        distances = new NativeArray<int>(length, Allocator.Persistent);
        neighbourDirections = new NativeArray<int2>(
            new int2[] 
            { 
                new int2(1, 0), new int2(1, 1), new int2(0, 1), new int2(-1, 1), new int2(-1, 0), new int2(-1, -1), new int2(0, -1), new int2(1, -1), 
            }, 
            Allocator.Persistent);

        UpdateFloodFill();
    }

    private void OnDisable()
    {
        Events.OnBuildingDestroyed -= BuildingHandler_OnBuildingDestroyed;
        Events.OnBuildingRepaired -= BuildingHandler_OnBuildingRepaired;
        portalObjectData.OnObjectSpawned -= OnPortalPlaced;

        cells.Dispose();
        directions.Dispose();
        distances.Dispose(); 
        neighbourDirections.Dispose();
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

    private void BuildingHandler_OnBuildingDestroyed(Building building)
    {
        blacklistedBuildingPositions.Add(building.Index);

        UpdateFloodFill();
    }

    private void BuildingHandler_OnBuildingRepaired(Building building)
    {
        blacklistedBuildingPositions.Remove(building.Index);
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

    [Button]
    private async void UpdateFloodFill()
    {
        if (updateQueued)
        {
            return;
        }

        updateQueued = true;
        await UniTask.WaitWhile(() => updatingFloodFill);

        updatingFloodFill = true;
        updateQueued = false;

        for (int i = 0; i < cells.Length; i++)
        {
            cells[i] = new PathCell
            {
                Index = i,
                MovementCost = 1,
                IsTarget = i % 10 == 0,
                IsWalkable = true,
            };
        }

        DistanceJob distanceJob = new DistanceJob()
        {
            cells = cells,
            distances = distances,
            neighbourDirections = neighbourDirections,
            GridWidth = gridResolution.x,
        };

        jobHandle = distanceJob.Schedule();
        
        await UniTask.WaitUntil(() => jobHandle.IsCompleted);
        jobHandle.Complete();

        PathJob pathJob = new PathJob()
        {
            directions = directions,
            distances = distances,
            neighbourDirections = neighbourDirections,
            GridWidth = gridResolution.x,
        };
        jobHandle = pathJob.Schedule(gridResolution.y, 32);

        await UniTask.WaitUntil(() => jobHandle.IsCompleted);
        jobHandle.Complete();

        updatingFloodFill = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (directions == null || directions.Length <= 0 || !jobHandle.IsCompleted)
        {
            return;
        }

        Gizmos.color = Color.red;
        for (int i = 0; i < directions.Length; i++)
        {
            Vector3 pos = new Vector3(i % gridResolution.x, 5, i / gridResolution.x);
            Gizmos.DrawLine(pos, pos + new Vector3(directions[i].x, 0, directions[i].y));
        }
    }
}

public struct PathCell
{
    public bool IsWalkable;
    public bool IsTarget;
    public byte MovementCost;
    public int Index;
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
                distances[neighbour.Index] = distances[cell.Index] + neighbour.MovementCost;
                frontierQueue.Enqueue(neighbour);
                visited.Add(neighbour.Index);
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
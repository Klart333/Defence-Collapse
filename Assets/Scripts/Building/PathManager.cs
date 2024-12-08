﻿using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Unity.Jobs;
using System;

public class PathManager : Singleton<PathManager>
{
    public event Action GetUnitCount;

    [Title("Flood fill")]
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
    public NativeArray<int> UnitCounts;

    private readonly List<Portal> portals = new List<Portal>();

    private JobHandle jobHandle;

    public NativeArray<float2> Directions => directions;
    public float CellScale => cellScale;
    public int GridHeight => gridSize.y;
    public int GridWidth => gridSize.x;
    public float GridWorldWidth => GridWidth * CellScale;
    public float GridWorldHeight => GridHeight * CellScale;

    public PathSet BlockerPathSet { get; private set; }
    public PathSet TargetPathSet { get; private set; }
    public PathSet PathPathSet { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        BlockerPathSet = new PathSet();
        TargetPathSet = new PathSet();
        PathPathSet = new PathSet();
    }

    private void OnEnable()
    {
        portalObjectData.OnObjectSpawned += OnPortalPlaced;

        int length = gridSize.x * gridSize.y;
        cells = new NativeArray<PathCell>(length, Allocator.Persistent);
        directions = new NativeArray<float2>(length, Allocator.Persistent);
        distances = new NativeArray<int>(length, Allocator.Persistent);
        UnitCounts = new NativeArray<int>(length, Allocator.Persistent);
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

    private void Update() // DONT DO EVERY UPDATE, :P
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
        for (int i = 0; i < UnitCounts.Length; i++)
        {
            UnitCounts[i] = 0;
        }

        GetUnitCount?.Invoke();

        for (int i = 0; i < cells.Length; i++)
        {
            bool walkable = !BlockerPathSet.TargetIndexes.Contains(i); // USE SET DIRTY FOR THESE AND REBUILD ON NOW
            bool target = walkable && TargetPathSet.TargetIndexes.Contains(i);
            bool isPath = PathPathSet.TargetIndexes.Contains(i);

            byte movementCost = (byte)((isPath ? 1 : 10) + UnitCounts[i]);
            cells[i] = new PathCell
            {
                Index = i,
                MovementCost = movementCost,
                IsTarget = target,
                IsWalkable = walkable,
            };
        } 

        DistanceJob distanceJob = new DistanceJob()
        {
            directions = directions,
            cells = cells,
            distances = distances,
            neighbourDirections = neighbourDirections,
            GridWidth = gridSize.x,
        };

        jobHandle = distanceJob.Schedule();
        jobHandle.Complete();
/*
        PathJob pathJob = new PathJob()
        {
            directions = directions,
            distances = distances,
            neighbourDirections = neighbourDirections,
            GridWidth = gridSize.x,
        };
        jobHandle = pathJob.Schedule(gridSize.y, 32);
        jobHandle.Complete();*/
    }

    #region Debug

    private void OnDrawGizmosSelected()
    {
        if (directions == null || directions.Length <= 0)
        {
            return;
        }

        for (int i = 0; i < directions.Length; i++)
        {
            Gizmos.color = Color.Lerp(Color.red, Color.black, distances[i] / (float)2000);
            Vector3 pos = new Vector3(i % gridSize.x, 5, i / gridSize.x) * cellScale;
            //Handles.Label(pos, distances[i].ToString());
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
    public int Index;
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

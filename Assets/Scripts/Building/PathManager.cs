using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Unity.Jobs;
using System;

public class PathManager : Singleton<PathManager>
{
    public event Action GetPathInformation;

    [Title("Flood fill")]
    [SerializeField]
    private Vector2Int gridSize;

    [SerializeField]
    private float cellScale;

    [Title("Portal")]
    [SerializeField]
    private GroundObjectData portalObjectData;

    private NativeArray<bool> targetIndexes;
    private NativeArray<bool> notWalkableIndexes;
    private NativeArray<byte> movementCosts;
    
    private NativeArray<float2> directions;
    private NativeArray<int> distances;
    private NativeArray<int2> neighbourDirections;
    public NativeArray<byte> UnitCounts;

    private readonly List<Portal> portals = new List<Portal>();

    private JobHandle jobHandle;

    public NativeArray<float2> Directions => directions;
    public float CellScale => cellScale;
    public int GridHeight => gridSize.y;
    public int GridWidth => gridSize.x;
    public float GridWorldWidth => GridWidth * CellScale;
    public float GridWorldHeight => GridHeight * CellScale;

    public BoolPathSet BlockerPathSet { get; private set; }
    public BoolPathSet TargetPathSet { get; private set; }
    public BytePathSet PathPathSet { get; private set; }

    private void OnEnable()
    {
        portalObjectData.OnObjectSpawned += OnPortalPlaced;

        int length = gridSize.x * gridSize.y;
        targetIndexes = new NativeArray<bool>(length, Allocator.Persistent);
        notWalkableIndexes = new NativeArray<bool>(length, Allocator.Persistent);
        movementCosts = new NativeArray<byte>(length, Allocator.Persistent);
        directions = new NativeArray<float2>(length, Allocator.Persistent);
        distances = new NativeArray<int>(length, Allocator.Persistent);
        UnitCounts = new NativeArray<byte>(length, Allocator.Persistent);
        neighbourDirections = new NativeArray<int2>(new int2[] { new int2(1, 0), new int2(1, 1), new int2(0, 1), new int2(-1, 1), new int2(-1, 0), new int2(-1, -1), new int2(0, -1), new int2(1, -1), }, Allocator.Persistent);

        for (int i = 0; i < length; i++)
        {
            movementCosts[i] = 10;
        }
        
        BlockerPathSet = new BoolPathSet(notWalkableIndexes);
        GetPathInformation += BlockerPathSet.RebuildTargetHashSet;
        
        TargetPathSet = new BoolPathSet(targetIndexes);
        GetPathInformation += TargetPathSet.RebuildTargetHashSet;
        
        PathPathSet = new BytePathSet(movementCosts, -9);
        GetPathInformation += PathPathSet.RebuildTargetHashSet;
    }

    private void OnDisable()
    {
        portalObjectData.OnObjectSpawned -= OnPortalPlaced;

        targetIndexes.Dispose();
        notWalkableIndexes.Dispose();
        movementCosts.Dispose();
        directions.Dispose();
        distances.Dispose(); 
        UnitCounts.Dispose();
        neighbourDirections.Dispose();
        
        GetPathInformation -= BlockerPathSet.RebuildTargetHashSet;
        GetPathInformation -= TargetPathSet.RebuildTargetHashSet;
        GetPathInformation -= PathPathSet.RebuildTargetHashSet;
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
            movementCosts[i] -= UnitCounts[i];
            UnitCounts[i] = 0;
        }

        GetPathInformation?.Invoke();

        for (int i = 0; i < UnitCounts.Length; i++)
        {
            movementCosts[i] += UnitCounts[i];
        }

        PathJob pathJob = new PathJob()
        {
            directions = directions,
            distances = distances,
            movementCosts = movementCosts,
            targetIndexes = targetIndexes,
            notWalkableIndexes = notWalkableIndexes,
            neighbourDirections = neighbourDirections,
            GridWidth = gridSize.x,
            ArrayLength = distances.Length
        };

        jobHandle = pathJob.Schedule();
        jobHandle.Complete();
    }

    #region Debug

    private void OnDrawGizmosSelected()
    {
        if (directions.Length <= 0)
        {
            return;
        }

        for (int i = 0; i < directions.Length; i++)
        {
            Gizmos.color = Color.Lerp(Color.red, Color.black, distances[i] / (float)2000);
            // ReSharper disable once PossibleLossOfFraction
            Vector3 pos = new Vector3(i % gridSize.x, 1.0f / cellScale, i / gridSize.x) * cellScale;
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
        // ReSharper disable once PossibleLossOfFraction
        return new Vector2(index % GridWidth, Mathf.FloorToInt(index / GridWidth)) * CellScale;
    }

    #endregion
}
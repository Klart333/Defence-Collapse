using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Unity.Jobs;
using System;
using System.Linq;

public class PathManager : Singleton<PathManager>
{
    public event Action GetPathInformation;

    [Title("Flow Field")]
    [SerializeField]
    private Vector2Int gridSize;

    [SerializeField]
    private float cellScale;

    [SerializeField]
    private float updateFrequency = 1; 
    
    [Title("Portal")]
    [SerializeField]
    private GroundObjectData portalObjectData;

    private NativeArray<bool> targetIndexes;
    private NativeArray<bool> notWalkableIndexes;
    private NativeArray<byte> movementCosts;
    
    private NativeArray<byte> directions;
    private NativeArray<int> distances;
    private NativeArray<int2> neighbourDirections;
    public NativeArray<byte> UnitCounts;

    private readonly List<Portal> portals = new List<Portal>();

    private JobHandle jobHandle;
    
    private float updateTimer;

    public NativeArray<byte> Directions => directions;
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
        directions = new NativeArray<byte>(length, Allocator.Persistent);
        distances = new NativeArray<int>(length, Allocator.Persistent);
        UnitCounts = new NativeArray<byte>(length, Allocator.Persistent);
        neighbourDirections = new NativeArray<int2>(new[] { new int2(1, 0), new int2(1, 1), new int2(0, 1), new int2(-1, 1), new int2(-1, 0), new int2(-1, -1), new int2(0, -1), new int2(1, -1), }, Allocator.Persistent);

        for (int i = 0; i < length; i++)
        {
            movementCosts[i] = 50;
        }
        
        BlockerPathSet = new BoolPathSet(notWalkableIndexes);
        GetPathInformation += BlockerPathSet.RebuildTargetHashSet;
        
        TargetPathSet = new BoolPathSet(targetIndexes);
        GetPathInformation += TargetPathSet.RebuildTargetHashSet;
        
        PathPathSet = new BytePathSet(movementCosts, -45);
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

    private void Update() // DONT DO EVERY UPDATE :P
    {
        updateTimer += Time.deltaTime;
        if (updateTimer >= updateFrequency)
        {
            updateTimer = 0;
            UpdateFlowField();
        }
    }

    private void LateUpdate()
    {
        if (updateTimer == 0 && !jobHandle.IsCompleted)
        {
            jobHandle.Complete();
        }
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
        return (from t in portals where !t.Locked select t.transform.position).ToList();
    }

    private void UpdateFlowField()
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
            Directions = directions,
            Distances = distances,
            MovementCosts = movementCosts.AsReadOnly(),
            TargetIndexes = targetIndexes.AsReadOnly(),
            NotWalkableIndexes = notWalkableIndexes.AsReadOnly(),
            NeighbourDirections = neighbourDirections.AsReadOnly(),
            GridWidth = gridSize.x,
            ArrayLength = distances.Length
        };

        jobHandle = pathJob.Schedule();
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
            Vector3 pos = new Vector3(i % gridSize.x, 1.0f / cellScale, i / gridSize.x) * cellScale;
            float2 dir = PathManager.ByteToDirection(directions[i]);
            Gizmos.DrawLine(pos, pos + new Vector3(dir.x, 0, dir.y) * cellScale);
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
        return pos is { x: > 0, y: > 0 } && pos.x < GridWorldWidth && pos.y < GridWorldHeight;
    }

    public int GetIndex(float xPos, float zPos)
    {
        return GetIndex(xPos, zPos, CellScale, GridWidth);
    }

    public int GetIndex(Vector2 pos)
    {
        return GetIndex(pos.x, pos.y, CellScale, GridWidth);
    }

    public Vector2 GetPos(int index)
    {
        return new Vector2(index % GridWidth, Mathf.FloorToInt(index / GridWidth)) * CellScale;
    }

    #region Static

    public static int GetIndex(float xPos, float zPos, float cellScale, int gridWidth)
    {
        return Math.GetMultiple(xPos, cellScale) + Math.GetMultiple(zPos, cellScale) * gridWidth;
    }
    
    public static float2 ByteToDirection(byte directionByte)
    {
        float angleRad = (directionByte / 255f) * math.PI2; // Map byte to [0, 360) degrees
        return new float2(math.cos(angleRad), math.sin(angleRad));
    }
    
    public static float3 ByteToDirectionFloat3(byte directionByte, float y = 0)
    {
        float angleRad = (directionByte / 255f) * math.PI2; // Map byte to [0, 360) degrees
        return new float3(math.cos(angleRad), y, math.sin(angleRad));
    }

    #endregion
    
    #endregion
}
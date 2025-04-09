using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Unity.Jobs;
using System;
using WaveFunctionCollapse;

namespace Pathfinding
{
    public class PathManager : Singleton<PathManager>
    {
        public event Action GetPathInformation;
        public event Action OnPathRebuilt;

        [Title("Flow Field")]
        [SerializeField]
        private Vector2Int gridSize;

        [SerializeField]
        private float cellScale;

        [SerializeField]
        private float updateFrequency = 1;

        public NativeArray<short> MovementCosts;
        public NativeArray<short> Units;
        
        private NativePriorityQueue<PathJob.IndexDistance> PathJobQueue;
        private NativeArray<int2> neighbourDirections;
        private NativeArray<bool> notWalkableIndexes;
        private NativeArray<bool> targetIndexes;
        private NativeArray<byte> directions;
        private NativeArray<int> distances;

        private JobHandle jobHandle;

        private float updateTimer;

        public float GridWorldHeight => GridHeight * CellScale;
        public float GridWorldWidth => GridWidth * CellScale;
        public NativeArray<byte> Directions => directions;
        public float CellScale => cellScale;
        public int GridHeight => gridSize.y;
        public int GridWidth => gridSize.x;

        public BoolPathSet BlockerPathSet { get; private set; }
        public BoolPathSet TargetPathSet { get; private set; }
        public ShortPathSet PathPathSet { get; private set; }

        private void OnEnable()
        {
            int length = gridSize.x * gridSize.y;
            neighbourDirections = new NativeArray<int2>(new[] { new int2(1, 0), new int2(1, 1), new int2(0, 1), new int2(-1, 1), new int2(-1, 0), new int2(-1, -1), new int2(0, -1), new int2(1, -1), }, Allocator.Persistent);
            PathJobQueue = new NativePriorityQueue<PathJob.IndexDistance>(1024, Allocator.Persistent);
            notWalkableIndexes = new NativeArray<bool>(length, Allocator.Persistent);
            MovementCosts = new NativeArray<short>(length, Allocator.Persistent);
            targetIndexes = new NativeArray<bool>(length, Allocator.Persistent);
            directions = new NativeArray<byte>(length, Allocator.Persistent);
            distances = new NativeArray<int>(length, Allocator.Persistent);
            Units = new NativeArray<short>(length, Allocator.Persistent);

            for (int i = 0; i < length; i++)
            {
                MovementCosts[i] = 100;
            }

            BlockerPathSet = new BoolPathSet(notWalkableIndexes);
            GetPathInformation += BlockerPathSet.RebuildTargetHashSet;

            TargetPathSet = new BoolPathSet(targetIndexes);
            GetPathInformation += TargetPathSet.RebuildTargetHashSet;

            PathPathSet = new ShortPathSet(MovementCosts, -94);
            GetPathInformation += PathPathSet.RebuildTargetHashSet;
        }

        private void OnDisable()
        {
            neighbourDirections.Dispose();
            notWalkableIndexes.Dispose();
            MovementCosts.Dispose();
            targetIndexes.Dispose();
            PathJobQueue.Dispose();
            directions.Dispose();
            distances.Dispose();
            Units.Dispose();

            GetPathInformation -= BlockerPathSet.RebuildTargetHashSet;
            GetPathInformation -= TargetPathSet.RebuildTargetHashSet;
            GetPathInformation -= PathPathSet.RebuildTargetHashSet;
        }

        private void Update()
        {
            updateTimer += Time.deltaTime;
            if (updateTimer >= updateFrequency)
            {
                if (!jobHandle.IsCompleted)
                {
                    Debug.Log("Not Completed...");
                    jobHandle.Complete();
                }

                updateTimer = 0;
                UpdateFlowField().Forget(Debug.LogError);
            }
        }

        private async UniTask UpdateFlowField()
        {
            for (int i = 0; i < MovementCosts.Length; i++)
            {
                MovementCosts[i] -= Units[i];
                Units[i] = 0;
            }
            
            GetPathInformation?.Invoke();
            await UniTask.DelayFrame(2);

            PathJob pathJob = new PathJob()
            {
                Directions = directions,
                Distances = distances,
                MovementCosts = MovementCosts.AsReadOnly(),
                TargetIndexes = targetIndexes.AsReadOnly(),
                NotWalkableIndexes = notWalkableIndexes.AsReadOnly(),
                NeighbourDirections = neighbourDirections.AsReadOnly(),
                GridWidth = gridSize.x,
                ArrayLength = distances.Length,
                FrontierQueue = PathJobQueue,
            };

            jobHandle = pathJob.Schedule();
            jobHandle.Complete();
            
            OnPathRebuilt?.Invoke();
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
                Vector3 pos = GetPos(i).ToXyZ(1);
                if (directions[i] == byte.MaxValue)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireCube(pos, Vector3.one * cellScale);
                    continue;
                }
                
                Gizmos.color = Color.Lerp(Color.red, Color.black, distances[i] / (float)10000);
                float2 dir = ByteToDirection(directions[i]);
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

        private const float HALF_BUILDING_CELL = 0.25f;
        private const float FULL_BUILDING_CELL = 0.5f;
        public Vector2 GetPos(int index)
        {
            return new Vector2(index % GridWidth - FULL_BUILDING_CELL, Mathf.FloorToInt((float)index / GridWidth) - FULL_BUILDING_CELL) * CellScale;
        }

        #region Static

        
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
        
        public static int GetIndex(float xPos, float zPos, float cellScale, int gridWidth)
        {
            return Math.GetMultiple(xPos + HALF_BUILDING_CELL, cellScale) + Math.GetMultiple(zPos + HALF_BUILDING_CELL, cellScale) * gridWidth;
        }

        #endregion

        #endregion
    }
}
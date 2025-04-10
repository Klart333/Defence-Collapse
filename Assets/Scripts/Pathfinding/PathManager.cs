using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Unity.Jobs;
using System;

namespace Pathfinding
{
    public class PathManager : Singleton<PathManager>
    {
        public static readonly int2[] NeighbourDirections =
        {
            new int2(1, 0),
            new int2(1, 1),
            new int2(0, 1),
            new int2(-1, 1),
            new int2(-1, 0),
            new int2(-1, -1),
            new int2(0, -1),
            new int2(1, -1),
        };

        public event Action GetPathInformation;

        [Title("Flow Field")]
        [SerializeField]
        private Vector2Int gridSize;

        [SerializeField]
        private float cellScale;

        [Title("Reference")]
        [SerializeField]
        private GroundGenerator groundGenerator;

        public NativeArray<short> MovementCosts;
        public NativeArray<short> Units;
        
        private NativeArray<bool> notWalkableIndexes;
        private NativeArray<bool> targetIndexes;

        private NativeArray<byte> directions;
        private NativeArray<int> distances;

        private JobHandle jobHandle;

        private bool waitingForData;
        private int arrayLength;

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
            arrayLength = gridSize.x * gridSize.y;
            
            notWalkableIndexes = new NativeArray<bool>(arrayLength, Allocator.Persistent);
            MovementCosts = new NativeArray<short>(arrayLength, Allocator.Persistent);
            targetIndexes = new NativeArray<bool>(arrayLength, Allocator.Persistent);
            directions = new NativeArray<byte>(arrayLength, Allocator.Persistent);
            distances = new NativeArray<int>(arrayLength, Allocator.Persistent);
            Units = new NativeArray<short>(arrayLength, Allocator.Persistent);

            for (int i = 0; i < arrayLength; i++)
            {
                MovementCosts[i] = 100;
            }

            BlockerPathSet = new BoolPathSet(notWalkableIndexes);
            GetPathInformation += BlockerPathSet.RebuildTargetHashSet;

            TargetPathSet = new BoolPathSet(targetIndexes);
            GetPathInformation += TargetPathSet.RebuildTargetHashSet;

            PathPathSet = new ShortPathSet(MovementCosts, -94);
            GetPathInformation += PathPathSet.RebuildTargetHashSet;
            
            groundGenerator.OnLockedChunkGenerated += OnChunkGenerated;
        }

        private void OnDisable()
        {
            notWalkableIndexes.Dispose();
            MovementCosts.Dispose();
            targetIndexes.Dispose();
            directions.Dispose();
            distances.Dispose();
            Units.Dispose();

            GetPathInformation -= BlockerPathSet.RebuildTargetHashSet;
            GetPathInformation -= TargetPathSet.RebuildTargetHashSet;
            GetPathInformation -= PathPathSet.RebuildTargetHashSet;
            
            groundGenerator.OnLockedChunkGenerated -= OnChunkGenerated;
        }

        private void Update()
        {
            if (!waitingForData)
            {
                UpdateFlowField().Forget(Debug.LogError);
            }
        }
        
        private void OnChunkGenerated(Chunk chunk)
        {
            
        }
        
        private async UniTask UpdateFlowField()
        {
            waitingForData = true;
            for (int i = 0; i < MovementCosts.Length; i++)
            {
                MovementCosts[i] -= Units[i];
                Units[i] = 0;
            }
            
            GetPathInformation?.Invoke();
            await UniTask.DelayFrame(2);

            new PathJob
            {
                NotWalkableIndexes = notWalkableIndexes.AsReadOnly(),
                MovementCosts = MovementCosts.AsReadOnly(),
                TargetIndexes = targetIndexes.AsReadOnly(),
                ArrayLength = arrayLength,
                Directions = directions,
                GridWidth = gridSize.x,
                Distances = distances,
                BatchSize = gridSize.x,
            }.Schedule(arrayLength, gridSize.x).Complete();
            waitingForData = false;
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
using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using Unity.Collections;
using Unity.Mathematics;
using Pathfinding.ECS;
using Unity.Entities;
using Gameplay.Event;
using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using System;

namespace Pathfinding
{
    public class PathManager : Singleton<PathManager>
    {
        public event Action GetPathInformation;

        [Title("Debug")]
        [SerializeField]
        private Gradient debugGradient;
        
        [SerializeField]
        private float pathUpdateInterval = 0.1f;
        
        private BlobAssetReference<PathChunkArray> pathChunks;
        private NativeHashMap<int2, int> chunkIndexToListIndex;

        private EntityManager entityManager;
        private Entity blobEntity;

        private float updateTimer;
        private int jobStartIndex;
        private int chunkAmount;

        public BuildingTargetPathSet TargetTargetPathSet { get; private set; }
        public ExtraDistancePathSet ExtraDistanceSet { get; private set; }
        public BoolPathSet NorthEdgeBarricadeSet { get; private set; }
        public BoolPathSet WestEdgeBarricadeSet { get; private set; }
        public IntPathSet BarricadePathSet { get; private set; }
        public BoolPathSet BlockerPathSet { get; private set; }

        private void OnEnable()
        {
            chunkIndexToListIndex = new NativeHashMap<int2, int>(200, Allocator.Persistent);
            
            chunkAmount = 0;
            chunkIndexToListIndex.Add(int2.zero, chunkAmount);    
            pathChunks = PathUtility.CreatePathChunks(++chunkAmount, int2.zero, default);
            
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            blobEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(blobEntity, new PathBlobber
            {
                PathBlob = pathChunks,
                ChunkIndexToListIndex = chunkIndexToListIndex.AsReadOnly(),
            });
            
            BlockerPathSet = new BoolPathSet(index => ref pathChunks.Value.PathChunks[chunkIndexToListIndex[index]].NotWalkableIndexes);
            GetPathInformation += BlockerPathSet.RebuildTargetHashSet;

            TargetTargetPathSet = new BuildingTargetPathSet(index => ref pathChunks.Value.PathChunks[chunkIndexToListIndex[index]].TargetIndexes);
            GetPathInformation += TargetTargetPathSet.RebuildTargetHashSet;
            
            BarricadePathSet = new IntPathSet(index => ref pathChunks.Value.PathChunks[chunkIndexToListIndex[index]].MovementCosts, 3_000);
            GetPathInformation += BarricadePathSet.RebuildTargetHashSet;
            
            ExtraDistanceSet = new ExtraDistancePathSet(index => ref pathChunks.Value.PathChunks[chunkIndexToListIndex[index]].ExtraDistance, 200);
            GetPathInformation += ExtraDistanceSet.RebuildTargetHashSet;

            NorthEdgeBarricadeSet = new BoolPathSet(index => ref pathChunks.Value.PathChunks[chunkIndexToListIndex[index]].NorthEdgeBlocks);
            GetPathInformation += NorthEdgeBarricadeSet.RebuildTargetHashSet;
            
            WestEdgeBarricadeSet = new BoolPathSet(index => ref pathChunks.Value.PathChunks[chunkIndexToListIndex[index]].WestEdgeBlocks);
            GetPathInformation += WestEdgeBarricadeSet.RebuildTargetHashSet;
            
            Events.OnGroundChunkGenerated += OnChunkGenerated;
        }
        
        private void OnDisable()
        {
            chunkIndexToListIndex.Dispose();
            pathChunks.Dispose();
            
            GetPathInformation -= BlockerPathSet.RebuildTargetHashSet;
            GetPathInformation -= TargetTargetPathSet.RebuildTargetHashSet;
            GetPathInformation -= BarricadePathSet.RebuildTargetHashSet;
            GetPathInformation -= ExtraDistanceSet.RebuildTargetHashSet;
            GetPathInformation -= NorthEdgeBarricadeSet.RebuildTargetHashSet;
            GetPathInformation -= WestEdgeBarricadeSet.RebuildTargetHashSet;
            
            Events.OnGroundChunkGenerated -= OnChunkGenerated;
        }

        private void LateUpdate()
        {
            updateTimer += Time.deltaTime;

            if (updateTimer > pathUpdateInterval)
            {
                updateTimer = 0;
                UpdateFlowField();
            }
        }
        
        private void OnChunkGenerated(Chunk chunk)
        {
            chunkIndexToListIndex.Add(chunk.ChunkIndex.xz, chunkAmount);
            pathChunks = PathUtility.CreatePathChunks(++chunkAmount, chunk.ChunkIndex.xz, pathChunks);
            
            entityManager.AddComponentData(blobEntity, new PathBlobber
            {
                PathBlob = pathChunks,
                ChunkIndexToListIndex = chunkIndexToListIndex.AsReadOnly(),
            });
        }
        
        private void UpdateFlowField()
        {
            GetPathInformation?.Invoke();

            const int quadrantWidth = PathUtility.GRID_WIDTH / 2;
            const int quadrantHeight = PathUtility.GRID_WIDTH / 2;
            int chunksLength = pathChunks.Value.PathChunks.Length;

            for (int i = 0; i < 4; i++)
            {
                int2 start = i switch
                {
                    0 => new int2(0, 0),
                    1 => new int2(quadrantWidth, 0),
                    2 => new int2(0, quadrantHeight),
                    3 => new int2(quadrantWidth, quadrantHeight),
                    _ => throw new ArgumentOutOfRangeException()
                };
                
                PathJob job = new PathJob
                {
                    StartX = start.x,
                    StartY = start.y,
                    PathChunks = pathChunks,
                    ChunkIndexToListIndex = chunkIndexToListIndex.AsReadOnly(),
                    QuadrantHeight = quadrantWidth,
                    QuadrantWidth = quadrantWidth,
                };
                JobHandle handle = job.ScheduleParallelByRef(chunksLength, chunksLength, default);
                handle.Complete();
            }
        }
        
        public bool TryMoveAlongFlowField(PathIndex pathIndex, float3 pathPosition, out PathIndex movedPathIndex, out float3 movedPathPosition)
        {
            ref PathChunk valuePathChunk = ref pathChunks.Value.PathChunks[chunkIndexToListIndex[pathIndex.ChunkIndex]];
            int combinedIndex = pathIndex.GridIndex.x + pathIndex.GridIndex.y * PathUtility.GRID_WIDTH;

            float2 direction = PathUtility.ByteToDirection(valuePathChunk.Directions[combinedIndex]);
            
            movedPathPosition = pathPosition + (math.round(direction) * PathUtility.CELL_SCALE).XyZ();
            movedPathIndex = PathUtility.GetIndex(movedPathPosition.xz);
            return chunkIndexToListIndex.ContainsKey(movedPathIndex.ChunkIndex);
        }
        
        #region Debug

        private void OnDrawGizmosSelected()
        {
            if (!pathChunks.IsCreated)
            {
                return;
            }
    
            for (int i = 0; i < chunkAmount; i++)
            {
                ref PathChunk pathChunk = ref pathChunks.Value.PathChunks[i];
                for (int x = 0; x < PathUtility.GRID_WIDTH; x++)
                for (int y = 0; y < PathUtility.GRID_WIDTH; y++)
                {
                    PathIndex index = new PathIndex(pathChunk.ChunkIndex, new int2(x, y));
                    int combinedIndex = x + y * PathUtility.GRID_WIDTH;
                    Vector3 pos = (Vector3)PathUtility.GetPos(index) + Vector3.up * 0.1f;
                    if (pathChunk.Directions[combinedIndex] == byte.MaxValue)
                    {
                        Gizmos.color = Color.green;
                        Gizmos.DrawWireCube(pos, Vector3.one * PathUtility.CELL_SCALE);
                        continue;
                    }
        
                    Gizmos.color = debugGradient.Evaluate(pathChunk.Distances[combinedIndex] / 100_000f);
                    float2 dir = PathUtility.ByteToDirection(pathChunk.Directions[combinedIndex]);
                    Vector3 direction = new Vector3(dir.x, 0, dir.y);
            
                    // Main line (shorter)
                    const float mainLineLength = PathUtility.CELL_SCALE * 0.7f;
                    Gizmos.DrawLine(pos, pos + direction * mainLineLength);
            
                    // Arrowhead lines
                    const float arrowHeadLength = PathUtility.CELL_SCALE * 0.3f;
                    const float arrowHeadAngle = 30f; // degrees
            
                    // Calculate perpendicular directions for arrowhead
                    Quaternion leftRot = Quaternion.Euler(0, -arrowHeadAngle, 0);
                    Quaternion rightRot = Quaternion.Euler(0, arrowHeadAngle, 0);
            
                    Vector3 arrowLeft = leftRot * direction * arrowHeadLength;
                    Vector3 arrowRight = rightRot * direction * arrowHeadLength;
            
                    // Draw arrowhead lines
                    Vector3 arrowStart = pos + direction * mainLineLength;
                    Gizmos.DrawLine(arrowStart, arrowStart + arrowLeft);
                    Gizmos.DrawLine(arrowStart, arrowStart + arrowRight);
                }
            }
        }

        #endregion
    }

    [BurstCompile]
    public readonly struct PathIndex : IEquatable<PathIndex>
    {
        public readonly int2 ChunkIndex;
        public readonly int2 GridIndex;

        public PathIndex(int2 chunkIndex, int2 gridIndex)
        {
            ChunkIndex = chunkIndex;
            GridIndex = gridIndex;
        }

        [BurstCompile]
        public bool Equals(PathIndex other)
        {
            return ChunkIndex.Equals(other.ChunkIndex) && GridIndex.Equals(other.GridIndex);
        }

        [BurstCompile]
        public override bool Equals(object obj)
        {
            return obj is PathIndex other && Equals(other);
        }

        [BurstCompile]
        public override int GetHashCode()
        {
            return HashCode.Combine(ChunkIndex, GridIndex);
        }

        public override string ToString()
        {
            return $"({ChunkIndex}, {GridIndex})";
        }
    }

    public struct PathChunkArray
    {
        public BlobArray<PathChunk> PathChunks;
    }
    
    public struct PathChunk
    {
        public BlobArray<bool> NorthEdgeBlocks;
        public BlobArray<bool> WestEdgeBlocks;
        
        public BlobArray<bool> NotWalkableIndexes;
        public BlobArray<bool> IndexOccupied;
        
        public BlobArray<int> MovementCosts;
        public BlobArray<int> ExtraDistance;
        
        public BlobArray<byte> TargetIndexes;
        public BlobArray<byte> Directions;
        public BlobArray<int> Distances;
        
        public int2 ChunkIndex;
    }
}
﻿using System.Collections.Generic;
using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using Unity.Collections;
using Unity.Mathematics;
using Pathfinding.ECS;
using Unity.Entities;
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
        
        [Title("Debug")]
        [SerializeField]
        private Gradient debugGradient;
        
        private readonly Dictionary<int, int2> listIndexToChunkIndex = new Dictionary<int, int2>();
        private BlobAssetReference<PathChunkArray> pathChunks;
        private NativeHashMap<int2, int> chunkIndexToListIndex;

        private EntityManager entityManager;
        private Entity blobEntity;

        private int arrayLength;
        private int chunkAmount;
        private int jobStartIndex;

        public NativeHashMap<int2, int> ChunkIndexToListIndex => chunkIndexToListIndex;
        public BlobAssetReference<PathChunkArray> PathChunks => pathChunks;
        public float CellScale => cellScale;

        public BuildingTargetPathSet TargetTargetPathSet { get; private set; }
        public ExtraDistancePathSet ExtraDistanceSet { get; private set; }
        public IntPathSet BarricadePathSet { get; private set; }
        public BoolPathSet BlockerPathSet { get; private set; }

        private void OnEnable()
        {
            arrayLength = gridSize.x * gridSize.y;
            
            chunkIndexToListIndex = new NativeHashMap<int2, int>(200, Allocator.Persistent);
            
            chunkAmount = 0;
            chunkIndexToListIndex.Add(int2.zero, chunkAmount);    
            listIndexToChunkIndex.Add(chunkAmount, int2.zero);
            pathChunks = CreatePathChunks(++chunkAmount, default);
            
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
            
            groundGenerator.OnLockedChunkGenerated += OnChunkGenerated;
        }
        
        private void OnDisable()
        {
            chunkIndexToListIndex.Dispose();
            pathChunks.Dispose();
            
            GetPathInformation -= BlockerPathSet.RebuildTargetHashSet;
            GetPathInformation -= TargetTargetPathSet.RebuildTargetHashSet;
            GetPathInformation -= BarricadePathSet.RebuildTargetHashSet;
            GetPathInformation -= ExtraDistanceSet.RebuildTargetHashSet;
            
            groundGenerator.OnLockedChunkGenerated -= OnChunkGenerated;
        }

        private void LateUpdate()
        {
            UpdateFlowField();
        }
        
        private void OnChunkGenerated(Chunk chunk)
        {
            chunkIndexToListIndex.Add(chunk.ChunkIndex.xz, chunkAmount);
            listIndexToChunkIndex.Add(chunkAmount, chunk.ChunkIndex.xz);
            pathChunks = CreatePathChunks(++chunkAmount, pathChunks);
            
            entityManager.AddComponentData(blobEntity, new PathBlobber
            {
                PathBlob = pathChunks,
                ChunkIndexToListIndex = chunkIndexToListIndex.AsReadOnly(),
            });
        }
        
        private BlobAssetReference<PathChunkArray> CreatePathChunks(int chunkAmount, BlobAssetReference<PathChunkArray> oldPathChunks)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref PathChunkArray pathChunkArray = ref builder.ConstructRoot<PathChunkArray>();

            BlobBuilderArray<PathChunk> arrayBuilder = builder.Allocate(
                ref pathChunkArray.PathChunks,
                chunkAmount
            );

            for (int i = 0; i < chunkAmount - 1; i++)
            {
                ref PathChunk newChunk = ref arrayBuilder[i];
                ref PathChunk oldChunk = ref oldPathChunks.Value.PathChunks[i];
                newChunk.ChunkIndex = oldChunk.ChunkIndex;
                
                BlobBuilderArray<int> units = builder.Allocate(ref newChunk.Units, arrayLength);
                for (int j = 0; j < arrayLength; j++)
                {
                    units[j] = oldChunk.Units[j];
                }
                
                BlobBuilderArray<int> distances = builder.Allocate(ref newChunk.Distances, arrayLength);
                for (int j = 0; j < arrayLength; j++)
                {
                    distances[j] = oldChunk.Distances[j];
                }
                
                BlobBuilderArray<byte> directions = builder.Allocate(ref newChunk.Directions, arrayLength);
                for (int j = 0; j < arrayLength; j++)
                {
                    directions[j] = oldChunk.Directions[j];
                }
                
                BlobBuilderArray<int> movementCosts = builder.Allocate(ref newChunk.MovementCosts, arrayLength);
                for (int j = 0; j < arrayLength; j++)
                {
                    movementCosts[j] = oldChunk.MovementCosts[j];
                }
                
                BlobBuilderArray<byte> targetIndexes = builder.Allocate(ref newChunk.TargetIndexes, arrayLength);
                for (int j = 0; j < arrayLength; j++)
                {
                    targetIndexes[j] = oldChunk.TargetIndexes[j];
                }
                
                BlobBuilderArray<bool> notWalkableIndexes = builder.Allocate(ref newChunk.NotWalkableIndexes, arrayLength);
                for (int j = 0; j < arrayLength; j++)
                {
                    notWalkableIndexes[j] = oldChunk.NotWalkableIndexes[j];
                }
                
                BlobBuilderArray<int> extraDistance = builder.Allocate(ref newChunk.ExtraDistance, arrayLength);
                for (int j = 0; j < arrayLength; j++)
                {
                    extraDistance[j] = oldChunk.ExtraDistance[j];
                }
            }
            
            ref PathChunk pathChunk = ref arrayBuilder[chunkAmount - 1];
            BuildPathChunk(ref pathChunk, chunkAmount - 1);
            
            BlobAssetReference<PathChunkArray> result = builder.CreateBlobAssetReference<PathChunkArray>(Allocator.Persistent);
            builder.Dispose();
            if (oldPathChunks.IsCreated)
            {
                oldPathChunks.Dispose();
            }
            return result;
            
            void BuildPathChunk(ref PathChunk pathChunk, int index)
            {
                pathChunk.ChunkIndex = listIndexToChunkIndex[index];
                builder.Allocate(ref pathChunk.Units, arrayLength);
                builder.Allocate(ref pathChunk.Directions, arrayLength);
                builder.Allocate(ref pathChunk.TargetIndexes, arrayLength);
                builder.Allocate(ref pathChunk.NotWalkableIndexes, arrayLength);
                builder.Allocate(ref pathChunk.ExtraDistance, arrayLength);
                BlobBuilderArray<int> distances = builder.Allocate(ref pathChunk.Distances, arrayLength);
                BlobBuilderArray<int> movements = builder.Allocate(ref pathChunk.MovementCosts, arrayLength);

                for (int i = 0; i < arrayLength; i++)
                {
                    distances[i] = 1000000;
                    movements[i] = 100;
                }
            }
        }
        
        private void UpdateFlowField()
        {
            GetPathInformation?.Invoke();

            int quadrantWidth = GRID_WIDTH / 2;
            int quadrantHeight = GRID_WIDTH / 2;
            int chunksLength = pathChunks.Value.PathChunks.Length;

            for (int i = 3; i >= 0; i--)
            {
                int start = i switch
                {
                    0 => 0,
                    1 => quadrantWidth,
                    2 => quadrantWidth * quadrantHeight * 2,
                    3 => quadrantWidth * quadrantHeight * 2 + quadrantWidth,
                    _ => throw new ArgumentOutOfRangeException()
                };
                
                var job = new PathJob
                {
                    Start = start,
                    PathChunks = pathChunks,
                    ChunkIndexToListIndex = chunkIndexToListIndex.AsReadOnly(),
                    QuadrantHeight = quadrantWidth,
                    QuadrantWidth = quadrantWidth,
                };
                JobHandle handle = job.ScheduleParallelByRef(chunksLength, chunksLength, default);
                handle.Complete();
            }
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
                for (int j = 0; j < arrayLength; j++)
                {
                    PathIndex index = new PathIndex(pathChunk.ChunkIndex, j);
                    Vector3 pos = (Vector3)GetPos(index) + Vector3.up * 0.1f;
                    if (pathChunk.Directions[j] == byte.MaxValue)
                    {
                        Gizmos.color = Color.green;
                        Gizmos.DrawWireCube(pos, Vector3.one * cellScale);
                        continue;
                    }
        
                    Gizmos.color = debugGradient.Evaluate(pathChunk.Distances[j] / 100_000f);
                    float2 dir = ByteToDirection(pathChunk.Directions[j]);
                    Vector3 direction = new Vector3(dir.x, 0, dir.y);
            
                    // Main line (shorter)
                    float mainLineLength = cellScale * 0.7f;
                    Gizmos.DrawLine(pos, pos + direction * mainLineLength);
            
                    // Arrowhead lines
                    float arrowHeadLength = cellScale * 0.3f;
                    float arrowHeadAngle = 30f; // degrees
            
                    // Calculate perpendicular directions for arrowhead
                    Quaternion leftRot = Quaternion.Euler(0, arrowHeadAngle, 0);
                    Quaternion rightRot = Quaternion.Euler(0, -arrowHeadAngle, 0);
            
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

        #region Utility
        
        public const float HALF_BUILDING_CELL = 0.25f;
        public const float FULL_BUILDING_CELL = 0.5f;
        public const float CELL_SCALE = 0.5f;
        public const float CHUNK_SIZE = 8;
        public const int GRID_WIDTH = 16; // Also change GetNeighbours inside PathJob
        public const int GRID_LENGTH = 256; // Width * Width
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
        
        public static float3 ChunkIndexToWorld(int2 chunkIndex)
        {
            return new float3(chunkIndex.x * CHUNK_SIZE, 0, chunkIndex.y * CHUNK_SIZE);
        }
        
        public static float3 GetPos(PathIndex index)
        {
            float3 chunkPos = ChunkIndexToWorld(index.ChunkIndex);
            float3 gridPos = new float3(index.GridIndex % GRID_WIDTH - FULL_BUILDING_CELL, 0, Mathf.FloorToInt((float)index.GridIndex / GRID_WIDTH) - FULL_BUILDING_CELL) * CELL_SCALE;
            
            return chunkPos + gridPos;
        }
        
        public static PathIndex GetIndex(float2 pos) => GetIndex(pos.x, pos.y);
        
        public static PathIndex GetIndex(float xPos, float zPos)
        {
            // Find which chunk the position is in
            int chunkZ = Utility.Math.GetMultipleFloored(zPos + FULL_BUILDING_CELL, CHUNK_SIZE);
            int chunkX = Utility.Math.GetMultipleFloored(xPos + FULL_BUILDING_CELL, CHUNK_SIZE);
            int2 chunkIndex = new int2(chunkX, chunkZ);
        
            // Calculate position relative to the chunk's origin
            float localX = xPos - chunkX * CHUNK_SIZE;
            float localZ = zPos - chunkZ * CHUNK_SIZE;
        
            // Find grid indices within the chunk
            int gridX = Utility.Math.GetMultiple(localX + HALF_BUILDING_CELL, CELL_SCALE);
            int gridZ = Utility.Math.GetMultiple(localZ + HALF_BUILDING_CELL, CELL_SCALE);
        
            // Convert 2D grid index to 1D
            int targetIndex = gridZ * GRID_WIDTH + gridX;
        
            return new PathIndex(chunkIndex, targetIndex);
        }

        #endregion
    }

    public readonly struct PathIndex : IEquatable<PathIndex>
    {
        public readonly int2 ChunkIndex;
        public readonly int GridIndex;

        public PathIndex(int2 chunkIndex, int gridIndex)
        {
            ChunkIndex = chunkIndex;
            GridIndex = gridIndex;
        }

        public bool Equals(PathIndex other)
        {
            return ChunkIndex.Equals(other.ChunkIndex) && GridIndex.Equals(other.GridIndex);
        }

        public override bool Equals(object obj)
        {
            return obj is PathIndex other && Equals(other);
        }

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
        public BlobArray<int> MovementCosts;
        public BlobArray<int> Units;
        
        public BlobArray<bool> NotWalkableIndexes;
        public BlobArray<byte> TargetIndexes;
        public BlobArray<int> ExtraDistance;
        
        public BlobArray<byte> Directions;
        public BlobArray<int> Distances;
        
        public int2 ChunkIndex;
    }
}
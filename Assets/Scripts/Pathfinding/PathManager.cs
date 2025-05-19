using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Unity.Jobs;
using System;
using System.Collections.Generic;
using Pathfinding.ECS;
using Unity.Entities;

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
        public float GridWorldHeight => gridSize.y * CellScale;
        public float GridWorldWidth => gridSize.x * CellScale;
        public int ArrayLength => arrayLength;
        public float CellScale => cellScale;
        public Vector2Int GridSize => gridSize;

        public BoolPathSet BlockerPathSet { get; private set; }
        public BytePathSet TargetPathSet { get; private set; }
        public IntPathSet PathPathSet { get; private set; }

        private void OnEnable()
        {
            arrayLength = gridSize.x * gridSize.y;
            
            chunkIndexToListIndex = new NativeHashMap<int2, int>(1000, Allocator.Persistent);
            
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

            TargetPathSet = new BytePathSet(index => ref pathChunks.Value.PathChunks[chunkIndexToListIndex[index]].TargetIndexes);
            GetPathInformation += TargetPathSet.RebuildTargetHashSet;

            PathPathSet = new IntPathSet(index => ref pathChunks.Value.PathChunks[chunkIndexToListIndex[index]].MovementCosts, -94);
            GetPathInformation += PathPathSet.RebuildTargetHashSet;
            
            groundGenerator.OnLockedChunkGenerated += OnChunkGenerated;
        }
        
        private void OnDisable()
        {
            chunkIndexToListIndex.Dispose();
            pathChunks.Dispose();
            
            GetPathInformation -= BlockerPathSet.RebuildTargetHashSet;
            GetPathInformation -= TargetPathSet.RebuildTargetHashSet;
            GetPathInformation -= PathPathSet.RebuildTargetHashSet;
            
            groundGenerator.OnLockedChunkGenerated -= OnChunkGenerated;
        }

        private void Update()
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
                
                BlobBuilderArray<short> units = builder.Allocate(ref newChunk.Units, arrayLength);
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
            
            new PathJob
            {
                PathChunks = pathChunks,
                ChunkIndexToListIndex = chunkIndexToListIndex.AsReadOnly(),
                Start = jobStartIndex,
                ArrayLength = arrayLength,
                GridWidth = gridSize.x,
                GridHeight = gridSize.y,
                ChunkAmount = chunkAmount,
            }.Schedule().Complete();

            jobStartIndex = (jobStartIndex + 1) % chunkAmount;
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
                
                    Gizmos.color = Color.Lerp(Color.red, Color.black, pathChunk.Distances[j] / (float)10000);
                    float2 dir = ByteToDirection(pathChunk.Directions[j]);
                    Gizmos.DrawLine(pos, pos + new Vector3(dir.x, 0, dir.y) * cellScale);
                }
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
        
        #region Static
        
        public const float HALF_BUILDING_CELL = 0.25f;
        public const float FULL_BUILDING_CELL = 0.5f;
        public const float CELL_SCALE = 0.5f;
        public const float CHUNK_SIZE = 12;
        public const int GRID_WIDTH = 24; // Also change GetNeighbours inside PathJob
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
            int chunkX = Math.GetMultipleFloored(xPos + FULL_BUILDING_CELL, CHUNK_SIZE);
            int chunkZ = Math.GetMultipleFloored(zPos + FULL_BUILDING_CELL, CHUNK_SIZE);
            int2 chunkIndex = new int2(chunkX, chunkZ);
        
            // Calculate position relative to the chunk's origin
            float localX = xPos - chunkX * CHUNK_SIZE;
            float localZ = zPos - chunkZ * CHUNK_SIZE;
        
            // Find grid indices within the chunk
            int gridX = Math.GetMultiple(localX + HALF_BUILDING_CELL, CELL_SCALE);
            int gridZ = Math.GetMultiple(localZ + HALF_BUILDING_CELL, CELL_SCALE);
        
            // Convert 2D grid index to 1D
            int targetIndex = gridZ * GRID_WIDTH + gridX;
        
            return new PathIndex(chunkIndex, targetIndex);
        }

        #endregion

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
        public BlobArray<short> Units;
        
        public BlobArray<bool> NotWalkableIndexes;
        public BlobArray<byte> TargetIndexes;
        
        public BlobArray<byte> Directions;
        public BlobArray<int> Distances;
        
        public int2 ChunkIndex;
    }
}
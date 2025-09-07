using System.Runtime.CompilerServices;
using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using Effects.LittleDudes;
using Unity.Collections;
using Unity.Mathematics;
using Pathfinding.ECS;
using Unity.Entities;
using UnityEngine;
using Unity.Jobs;
using System;
using Gameplay.Event;

namespace Pathfinding
{
    public class PathManager : Singleton<PathManager>
    {
        public event Action GetPathInformation;

        [Title("Flow Field")]
        [SerializeField]
        private float cellScale;

        [Title("Debug")]
        [SerializeField]
        private Gradient debugGradient;
        
        private BlobAssetReference<PathChunkArray> pathChunks;
        private NativeHashMap<int2, int> chunkIndexToListIndex;

        private EntityManager entityManager;
        private Entity blobEntity;

        private int chunkAmount;
        private int jobStartIndex;

        public float CellScale => cellScale;

        public BuildingTargetPathSet TargetTargetPathSet { get; private set; }
        public ExtraDistancePathSet ExtraDistanceSet { get; private set; }
        public IntPathSet BarricadePathSet { get; private set; }
        public BoolPathSet BlockerPathSet { get; private set; }

        private void OnEnable()
        {
            chunkIndexToListIndex = new NativeHashMap<int2, int>(200, Allocator.Persistent);
            
            chunkAmount = 0;
            chunkIndexToListIndex.Add(int2.zero, chunkAmount);    
            pathChunks = PathUtility.CreatePathChunks(++chunkAmount, int2.zero, (BlobAssetReference<PathChunkArray>)default);
            
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
            
            Events.OnGroundChunkGenerated -= OnChunkGenerated;
        }

        private void LateUpdate()
        {
            UpdateFlowField();
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

            int quadrantWidth = PathUtility.GRID_WIDTH / 2;
            int quadrantHeight = PathUtility.GRID_WIDTH / 2;
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
                for (int j = 0; j < pathChunk.ExtraDistance.Length; j++)
                {
                    PathIndex index = new PathIndex(pathChunk.ChunkIndex, j);
                    Vector3 pos = (Vector3)PathUtility.GetPos(index) + Vector3.up * 0.1f;
                    if (pathChunk.Directions[j] == byte.MaxValue)
                    {
                        Gizmos.color = Color.green;
                        Gizmos.DrawWireCube(pos, Vector3.one * cellScale);
                        continue;
                    }
        
                    Gizmos.color = debugGradient.Evaluate(pathChunk.Distances[j] / 100_000f);
                    float2 dir = PathUtility.ByteToDirection(pathChunk.Directions[j]);
                    Vector3 direction = new Vector3(dir.x, 0, dir.y);
            
                    // Main line (shorter)
                    float mainLineLength = cellScale * 0.7f;
                    Gizmos.DrawLine(pos, pos + direction * mainLineLength);
            
                    // Arrowhead lines
                    float arrowHeadLength = cellScale * 0.3f;
                    float arrowHeadAngle = 30f; // degrees
            
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

    public static class PathUtility
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
        
        public const float HALF_BUILDING_CELL = 0.25f;
        public const float FULL_BUILDING_CELL = 0.5f;
        public const float CELL_SCALE = 1.0f;
        public const float CHUNK_SIZE = 8;
        public const int GRID_WIDTH = 8; 
        public const int GRID_LENGTH = GRID_WIDTH * GRID_WIDTH;
        
        public static float2 ByteToDirection(byte directionByte)
        {
            float angleRad = (directionByte / 255f) * math.PI2; // Map byte to [0, 360) degrees
            return new float2(math.cos(angleRad), math.sin(angleRad));
        }

        public static float3 ByteToDirectionFloat3(byte directionByte, float y = 0)
        {
            float angleRad = (directionByte / 255f) * math.PI2; // Map byte to [0, 360) degrees
            return math.normalize(new float3(math.cos(angleRad), y, math.sin(angleRad)));
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
        
        public static PathIndex AddToPathIndex(PathIndex index, int2 add)
        {
            int2 chunkIndexAdd = int2.zero;
            chunkIndexAdd.x = (index.GridIndex + add.x) switch
            {
                >= GRID_LENGTH => 1,
                < 0 => -1,
                _ => 0
            };
            
            chunkIndexAdd.y = (index.GridIndex + add.y * GRID_WIDTH) switch
            {
                >= GRID_LENGTH => 1,
                < 0 => -1,
                _ => 0
            };

            int flatAdd = add.x - chunkIndexAdd.x * GRID_WIDTH + add.y * GRID_WIDTH - chunkIndexAdd.y * GRID_LENGTH;
            return new PathIndex(index.ChunkIndex + chunkIndexAdd, index.GridIndex + flatAdd);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte GetDirection(int2 direction)
        {
            // Directly map the 8 possible int2 values to corresponding byte values
            return (direction.x, direction.y) switch
            {
                (1, 0) => 0,       // Right
                (1, 1) => 32,      // Up-Right
                (0, 1) => 64,      // Up
                (-1, 1) => 96,     // Up-Left
                (-1, 0) => 128,    // Left
                (-1, -1) => 160,   // Down-Left
                (0, -1) => 192,    // Down
                (1, -1) => 224,    // Down-Right
                _ => 0             
            };
        }
        
        public static BlobAssetReference<PathChunkArray> CreatePathChunks(int chunkAmount, int2 chunkIndex, BlobAssetReference<PathChunkArray> oldPathChunks)
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
                
                BlobBuilderArray<int> units = builder.Allocate(ref newChunk.Units, GRID_LENGTH);
                for (int j = 0; j < GRID_LENGTH; j++)
                {
                    units[j] = oldChunk.Units[j];
                }
                
                BlobBuilderArray<int> distances = builder.Allocate(ref newChunk.Distances, GRID_LENGTH);
                for (int j = 0; j < GRID_LENGTH; j++)
                {
                    distances[j] = oldChunk.Distances[j];
                }
                
                BlobBuilderArray<byte> directions = builder.Allocate(ref newChunk.Directions, GRID_LENGTH);
                for (int j = 0; j < GRID_LENGTH; j++)
                {
                    directions[j] = oldChunk.Directions[j];
                }
                
                BlobBuilderArray<int> movementCosts = builder.Allocate(ref newChunk.MovementCosts, GRID_LENGTH);
                for (int j = 0; j < GRID_LENGTH; j++)
                {
                    movementCosts[j] = oldChunk.MovementCosts[j];
                }
                
                BlobBuilderArray<byte> targetIndexes = builder.Allocate(ref newChunk.TargetIndexes, GRID_LENGTH);
                for (int j = 0; j < GRID_LENGTH; j++)
                {
                    targetIndexes[j] = oldChunk.TargetIndexes[j];
                }
                
                BlobBuilderArray<bool> notWalkableIndexes = builder.Allocate(ref newChunk.NotWalkableIndexes, GRID_LENGTH);
                for (int j = 0; j < GRID_LENGTH; j++)
                {
                    notWalkableIndexes[j] = oldChunk.NotWalkableIndexes[j];
                }
                
                BlobBuilderArray<int> extraDistance = builder.Allocate(ref newChunk.ExtraDistance, GRID_LENGTH);
                for (int j = 0; j < GRID_LENGTH; j++)
                {
                    extraDistance[j] = oldChunk.ExtraDistance[j];
                }
            }
            
            ref PathChunk pathChunk = ref arrayBuilder[chunkAmount - 1];
            BuildPathChunk(ref pathChunk);
            
            BlobAssetReference<PathChunkArray> result = builder.CreateBlobAssetReference<PathChunkArray>(Allocator.Persistent);
            builder.Dispose();
            if (oldPathChunks.IsCreated)
            {
                oldPathChunks.Dispose();
            }
            return result;
            
            void BuildPathChunk(ref PathChunk pathChunk)
            {
                pathChunk.ChunkIndex = chunkIndex;
                
                builder.Allocate(ref pathChunk.Units, GRID_LENGTH);
                builder.Allocate(ref pathChunk.Directions, GRID_LENGTH);
                builder.Allocate(ref pathChunk.TargetIndexes, GRID_LENGTH);
                builder.Allocate(ref pathChunk.NotWalkableIndexes, GRID_LENGTH);
                builder.Allocate(ref pathChunk.ExtraDistance, GRID_LENGTH);
                BlobBuilderArray<int> distances = builder.Allocate(ref pathChunk.Distances, GRID_LENGTH);
                BlobBuilderArray<int> movements = builder.Allocate(ref pathChunk.MovementCosts, GRID_LENGTH);

                for (int i = 0; i < GRID_LENGTH; i++)
                {
                    distances[i] = 1000000;
                    movements[i] = 100;
                }
            }
        }
        public static BlobAssetReference<LittleDudePathChunkArray> CreatePathChunks(int chunkAmount, int2 chunkIndex, BlobAssetReference<LittleDudePathChunkArray> oldPathChunks)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref LittleDudePathChunkArray littleDudePathChunkArray = ref builder.ConstructRoot<LittleDudePathChunkArray>();

            BlobBuilderArray<LittleDudePathChunk> arrayBuilder = builder.Allocate(
                ref littleDudePathChunkArray.PathChunks,
                chunkAmount
            );

            for (int i = 0; i < chunkAmount - 1; i++)
            {
                ref LittleDudePathChunk newChunk = ref arrayBuilder[i];
                ref LittleDudePathChunk oldChunk = ref oldPathChunks.Value.PathChunks[i];
                newChunk.ChunkIndex = oldChunk.ChunkIndex;
                
                BlobBuilderArray<int> distances = builder.Allocate(ref newChunk.Distances, GRID_LENGTH);
                for (int j = 0; j < GRID_LENGTH; j++)
                {
                    distances[j] = oldChunk.Distances[j];
                }
                
                BlobBuilderArray<byte> directions = builder.Allocate(ref newChunk.Directions, GRID_LENGTH);
                for (int j = 0; j < GRID_LENGTH; j++)
                {
                    directions[j] = oldChunk.Directions[j];
                }
                
                BlobBuilderArray<int> targetIndexes = builder.Allocate(ref newChunk.TargetIndexes, GRID_LENGTH);
                for (int j = 0; j < GRID_LENGTH; j++)
                {
                    targetIndexes[j] = oldChunk.TargetIndexes[j];
                }
            }
            
            ref LittleDudePathChunk littleDudePathChunk = ref arrayBuilder[chunkAmount - 1];
            BuildPathChunk(ref littleDudePathChunk);
            
            BlobAssetReference<LittleDudePathChunkArray> result = builder.CreateBlobAssetReference<LittleDudePathChunkArray>(Allocator.Persistent);
            builder.Dispose();
            if (oldPathChunks.IsCreated)
            {
                oldPathChunks.Dispose();
            }
            return result;
            
            void BuildPathChunk(ref LittleDudePathChunk pathChunk)
            {
                pathChunk.ChunkIndex = chunkIndex;
                
                builder.Allocate(ref pathChunk.Directions, GRID_LENGTH);
                builder.Allocate(ref pathChunk.TargetIndexes, GRID_LENGTH);
                BlobBuilderArray<int> distances = builder.Allocate(ref pathChunk.Distances, GRID_LENGTH);

                for (int i = 0; i < GRID_LENGTH; i++)
                {
                    distances[i] = 1000000;
                }
            }
        }
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
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Utility;

namespace Pathfinding
{
    [BurstCompile]
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
        
        // Right, Up, Left, Down
        public static readonly int2[] NeighbourDirectionsCardinal =
        {
            new int2(1, 0),
            new int2(0, 1),
            new int2(-1, 0),
            new int2(0, -1),
        };

        public const int CELL_SCALE = 2;
        public const int GRID_WIDTH = 4;

        public const int CHUNK_SIZE = GRID_WIDTH * CELL_SCALE;
        public const int GRID_LENGTH = GRID_WIDTH * GRID_WIDTH;
        public const int HALF_CELL_SCALE = CELL_SCALE / 2;
        
        public static float2 ByteToDirection(byte directionByte)
        {
            float angleRad = (directionByte / 255f) * math.PI2; // Map byte to [0, 360) degrees
            return new float2(math.cos(angleRad), math.sin(angleRad));
        }
        
        public static float3 GetPos(PathIndex index)
        {
            float3 chunkPos = new float3(index.ChunkIndex.x * CHUNK_SIZE, 0, index.ChunkIndex.y * CHUNK_SIZE);
            float3 gridPos = new float3(index.GridIndex.x * CELL_SCALE + HALF_CELL_SCALE, 0, index.GridIndex.y * CELL_SCALE + HALF_CELL_SCALE);
            
            return chunkPos + gridPos;
        }
        
        public static float2 GetPos(int2 gridIndex)
        {
            float2 gridPos = new float2(gridIndex.x * CELL_SCALE, gridIndex.y * CELL_SCALE);
            
            return gridPos;
        }
        
        public static PathIndex GetIndex(float2 pos) => GetIndex(pos.x, pos.y);
        
        public static PathIndex GetIndex(float xPos, float zPos)
        {
            // Find which chunk the position is in
            int chunkX = Math.GetMultipleFloored(xPos, CHUNK_SIZE);
            int chunkZ = Math.GetMultipleFloored(zPos, CHUNK_SIZE);
            int2 chunkIndex = new int2(chunkX, chunkZ);
        
            // Calculate position relative to the chunk's origin
            float localX = xPos - chunkX * CHUNK_SIZE;
            float localZ = zPos - chunkZ * CHUNK_SIZE;
        
            // Find grid indices within the chunk
            int gridX = Math.GetMultipleFloored(localX, CELL_SCALE);
            int gridZ = Math.GetMultipleFloored(localZ, CELL_SCALE);
        
            return new PathIndex(chunkIndex, new int2(gridX, gridZ));
        }

        public static int2 GetCombinedIndex(float2 pos)
        {
            int gridX = Math.GetMultipleFloored(pos.x, CELL_SCALE);
            int gridZ = Math.GetMultipleFloored(pos.y, CELL_SCALE);
        
            return new int2(gridX, gridZ);
        }
        
        public static int2 GetCombinedIndex(PathIndex index)
        {
            return new int2(index.ChunkIndex.x * GRID_WIDTH + index.GridIndex.x, index.ChunkIndex.y * GRID_WIDTH + index.GridIndex.y);
        }
        
        public static void NativeGetNeighbouringPathIndexes(NativeArray<int2> neighbours, int2 index)
        {
            for (int i = 0; i < NeighbourDirectionsCardinal.Length; i++)
            {
                neighbours[i] = index + NeighbourDirectionsCardinal[i];
            }
        }
        
        public static int GetDistance(int2 a, int2 b)
        {
            return math.abs(a.x - b.x) + math.abs(a.y - b.y);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte GetDirection(int2 direction)
        {
            // Directly map the 8 possible int2 values to corresponding byte values
            return (direction.x, direction.y) switch
            {
                (1, 0) => 0,       // Right
                //(1, 1) => 32,      // Up-Right
                (0, 1) => 64,      // Up
                //(-1, 1) => 96,     // Up-Left
                (-1, 0) => 128,    // Left
                //(-1, -1) => 160,   // Down-Left
                (0, -1) => 192,    // Down
                //(1, -1) => 224,    // Down-Right
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
                
                BlobBuilderArray<bool> indexOccupied = builder.Allocate(ref newChunk.IndexOccupied, GRID_LENGTH);
                for (int j = 0; j < GRID_LENGTH; j++)
                {
                    indexOccupied[j] = oldChunk.IndexOccupied[j];
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
                
                BlobBuilderArray<bool> northEdges = builder.Allocate(ref newChunk.NorthEdgeBlocks, GRID_LENGTH);
                for (int j = 0; j < GRID_LENGTH; j++)
                {
                    northEdges[j] = oldChunk.NorthEdgeBlocks[j];
                }
                
                BlobBuilderArray<bool> westEdges = builder.Allocate(ref newChunk.WestEdgeBlocks, GRID_LENGTH);
                for (int j = 0; j < GRID_LENGTH; j++)
                {
                    westEdges[j] = oldChunk.WestEdgeBlocks[j];
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
                
                builder.Allocate(ref pathChunk.NotWalkableIndexes, GRID_LENGTH);
                builder.Allocate(ref pathChunk.NorthEdgeBlocks, GRID_LENGTH);
                builder.Allocate(ref pathChunk.WestEdgeBlocks, GRID_LENGTH);
                builder.Allocate(ref pathChunk.IndexOccupied, GRID_LENGTH);
                builder.Allocate(ref pathChunk.TargetIndexes, GRID_LENGTH);
                builder.Allocate(ref pathChunk.ExtraDistance, GRID_LENGTH);
                builder.Allocate(ref pathChunk.Directions, GRID_LENGTH);
                BlobBuilderArray<int> distances = builder.Allocate(ref pathChunk.Distances, GRID_LENGTH);
                BlobBuilderArray<int> movements = builder.Allocate(ref pathChunk.MovementCosts, GRID_LENGTH);

                for (int i = 0; i < GRID_LENGTH; i++)
                {
                    distances[i] = 1000000;
                    movements[i] = 100;
                }
            }
        }
    }
}
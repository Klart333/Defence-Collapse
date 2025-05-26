using Unity.Collections;
using Unity.Mathematics;
using Unity.Entities;
using Pathfinding;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

[BurstCompile(FloatPrecision.Low, FloatMode.Fast, OptimizeFor = OptimizeFor.Performance)]
public struct PathJob : IJob
{
    public BlobAssetReference<PathChunkArray> PathChunks;

    [ReadOnly, NativeDisableContainerSafetyRestriction]
    public NativeHashMap<int2, int>.ReadOnly ChunkIndexToListIndex;

    public int Start;
    public int ArrayLength;
    public int ChunkAmount;
    
    [BurstCompile]
    public void Execute()
    {
        NativeArray<PathIndex> neighbours = new NativeArray<PathIndex>(8, Allocator.Temp);

        for (int i = Start; i < Start + 2; i++)
        {
            int index = i % ChunkAmount;
            CalculatePath(neighbours, ref PathChunks.Value.PathChunks[index]);
        }

        neighbours.Dispose();
    }
    
    private void CalculatePath(NativeArray<PathIndex> neighbours, ref PathChunk pathChunk)
    {
        for (int i = 0; i < ArrayLength; i++)
        {
            if (pathChunk.TargetIndexes[i] != 0)
            {
                pathChunk.Distances[i] = pathChunk.TargetIndexes[i] * 50; // 100 is the weight of one tile
                pathChunk.Directions[i] = byte.MaxValue;
                continue;
            }

            if (GetClosestNeighbour(neighbours, ref pathChunk, i, out int shortestDistance, out int dirIndex)) continue;
            
            pathChunk.Directions[i] = GetDirection(PathManager.NeighbourDirections[dirIndex]);
            if (pathChunk.NotWalkableIndexes[i])
            {
                pathChunk.Distances[i] = 1_000_000_000;  
            }
            else
            {
                pathChunk.Distances[i] = shortestDistance;
            }
        }
    }

    private bool GetClosestNeighbour(NativeArray<PathIndex> neighbours, ref PathChunk pathChunk, int gridIndex, out int shortestDistance, out int dirIndex)
    {
        int2 currentChunkIndex = pathChunk.ChunkIndex;
        GetNeighbours(currentChunkIndex, gridIndex, neighbours);
            
        shortestDistance = int.MaxValue;
        dirIndex = 0;
        for (int j = 0; j < 8; j++)
        {
            PathIndex neighbourIndex = neighbours[j];
            if (neighbourIndex.GridIndex == -1) continue;
                
            ref PathChunk neighbour = ref neighbourIndex.ChunkIndex.Equals(currentChunkIndex) 
                ? ref pathChunk 
                : ref PathChunks.Value.PathChunks[ChunkIndexToListIndex[neighbourIndex.ChunkIndex]];
                
            int manhattanDist = j % 2 == 0 ? 5 : 7;
            int dist = neighbour.Distances[neighbourIndex.GridIndex] + neighbour.MovementCosts[neighbourIndex.GridIndex] * manhattanDist;
            if (dist >= shortestDistance) continue;
                
            shortestDistance = dist;
            dirIndex = j;
        }

        return shortestDistance <= 0;
    }
    
    private void GetNeighbours(int2 chunkIndex, int gridIndex, NativeArray<PathIndex> array)
    {
        int x = gridIndex % PathManager.GRID_WIDTH;
        int y = gridIndex /  PathManager.GRID_WIDTH;

        for (int i = 0; i < 8; i++)
        {
            int2 dir = PathManager.NeighbourDirections[i];
            int2 neighbour = new int2(x + dir.x, y + dir.y);

            array[i] = neighbour switch // Grid width / height = 16, // NO DIAGONALS BUT IT'S FINE
            {
                {x: < 0} => ChunkIndexToListIndex.ContainsKey(new int2(chunkIndex.x - 1, chunkIndex.y)) 
                    ? new PathIndex(new int2(chunkIndex.x - 1, chunkIndex.y), 15 + y * PathManager.GRID_WIDTH )
                    : new PathIndex(default, -1),
                {x: > 15} => ChunkIndexToListIndex.ContainsKey(new int2(chunkIndex.x + 1, chunkIndex.y)) 
                    ? new PathIndex(new int2(chunkIndex.x + 1, chunkIndex.y), 0 + y * PathManager.GRID_WIDTH )
                    : new PathIndex(default, -1),
                {y: < 0} => ChunkIndexToListIndex.ContainsKey(new int2(chunkIndex.x, chunkIndex.y - 1)) 
                    ? new PathIndex(new int2(chunkIndex.x, chunkIndex.y - 1), x + 15 * PathManager.GRID_WIDTH  ) 
                    : new PathIndex(default, -1),
                {y: > 15} => ChunkIndexToListIndex.ContainsKey(new int2(chunkIndex.x, chunkIndex.y + 1)) 
                    ? new PathIndex(new int2(chunkIndex.x, chunkIndex.y + 1), x + 0 * PathManager.GRID_WIDTH ) 
                    : new PathIndex(default, -1),
                _ => new PathIndex(chunkIndex, neighbour.x + neighbour.y * PathManager.GRID_WIDTH),
            };
        }
    }

    private static byte GetDirection(int2 direction)
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
}
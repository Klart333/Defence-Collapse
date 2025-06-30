using Unity.Collections.LowLevel.Unsafe;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Entities;
using Pathfinding;
using Unity.Burst;
using Unity.Jobs;

[BurstCompile(FloatPrecision.Low, FloatMode.Fast, OptimizeFor = OptimizeFor.Performance)]
public struct PathJob : IJobFor
{
    [NativeDisableParallelForRestriction]
    public BlobAssetReference<PathChunkArray> PathChunks;

    [ReadOnly, NativeDisableContainerSafetyRestriction]
    public NativeHashMap<int2, int>.ReadOnly ChunkIndexToListIndex;

    public int Start;
    public int QuadrantWidth;
    public int QuadrantHeight;
    
    [BurstCompile]
    public void Execute(int index)
    {
        NativeArray<PathIndex> neighbours = new NativeArray<PathIndex>(8, Allocator.Temp);

        for (int y = 0; y < QuadrantHeight; y++)
        for (int x = QuadrantWidth * y; x < QuadrantWidth * (y + 1); x++)
        {
            int i = Start + y * QuadrantWidth + x;
            CalculatePathAtIndex(neighbours, ref PathChunks.Value.PathChunks[index], i);
        }

        neighbours.Dispose();
    }
    
    private void CalculatePathAtIndex(NativeArray<PathIndex> neighbours, ref PathChunk pathChunk, int index)
    {
        byte targetIndex = pathChunk.TargetIndexes[index];

        switch (targetIndex)
        {
            // Handle common case (~99%)
            case 0:
            {
                if (GetClosestNeighbour(neighbours, ref pathChunk, index, out int dist, out int dirIdx))
                {
                    pathChunk.Directions[index] = PathUtility.GetDirection(PathUtility.NeighbourDirections[dirIdx]);
                    pathChunk.Distances[index] = pathChunk.NotWalkableIndexes[index] ? 1_000_000_000 : dist;
                }
                return;
            }
            // Handle barricade case (targetIndex == 255)
            case byte.MaxValue:
            {
                if (GetClosestNeighbour(neighbours, ref pathChunk, index, out int dist, out _))
                {
                    pathChunk.Directions[index] = byte.MaxValue;
                    pathChunk.Distances[index] = dist;
                }
                return;
            }
            default:
                // Handle building case (targetIndex 1-254)
                pathChunk.Distances[index] = targetIndex * 50;
                pathChunk.Directions[index] = byte.MaxValue;
                break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            int dist = neighbour.Distances[neighbourIndex.GridIndex] 
                       + neighbour.MovementCosts[neighbourIndex.GridIndex] * manhattanDist 
                       + neighbour.ExtraDistance[neighbourIndex.GridIndex];
            
            if (dist >= shortestDistance) continue;
                
            shortestDistance = dist;
            dirIndex = j;
        }

        return shortestDistance < int.MaxValue;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GetNeighbours(int2 chunkIndex, int gridIndex, NativeArray<PathIndex> array)
    {
        int x = gridIndex % PathUtility.GRID_WIDTH;
        int y = gridIndex /  PathUtility.GRID_WIDTH;

        for (int i = 0; i < 8; i++)
        {
            int2 dir = PathUtility.NeighbourDirections[i];
            int2 neighbour = new int2(x + dir.x, y + dir.y);

            array[i] = neighbour switch // Grid width / height = 16, // NO DIAGONALS BUT IT'S FINE
            {
                {x: < 0} => ChunkIndexToListIndex.ContainsKey(new int2(chunkIndex.x - 1, chunkIndex.y)) 
                    ? new PathIndex(new int2(chunkIndex.x - 1, chunkIndex.y), PathUtility.GRID_WIDTH - 1 + y * PathUtility.GRID_WIDTH )
                    : new PathIndex(default, -1),
                {x: >= PathUtility.GRID_WIDTH} => ChunkIndexToListIndex.ContainsKey(new int2(chunkIndex.x + 1, chunkIndex.y)) 
                    ? new PathIndex(new int2(chunkIndex.x + 1, chunkIndex.y), 0 + y * PathUtility.GRID_WIDTH )
                    : new PathIndex(default, -1),
                {y: < 0} => ChunkIndexToListIndex.ContainsKey(new int2(chunkIndex.x, chunkIndex.y - 1)) 
                    ? new PathIndex(new int2(chunkIndex.x, chunkIndex.y - 1), x + (PathUtility.GRID_WIDTH - 1) * PathUtility.GRID_WIDTH  ) 
                    : new PathIndex(default, -1),
                {y: >= PathUtility.GRID_WIDTH} => ChunkIndexToListIndex.ContainsKey(new int2(chunkIndex.x, chunkIndex.y + 1)) 
                    ? new PathIndex(new int2(chunkIndex.x, chunkIndex.y + 1), x + 0 * PathUtility.GRID_WIDTH ) 
                    : new PathIndex(default, -1),
                _ => new PathIndex(chunkIndex, neighbour.x + neighbour.y * PathUtility.GRID_WIDTH),
            };
        }
    }
}
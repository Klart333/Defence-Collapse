using Unity.Collections.LowLevel.Unsafe;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Burst;
using Unity.Jobs;
using System;

namespace Pathfinding
{
    [BurstCompile(FloatPrecision.Low, FloatMode.Fast, OptimizeFor = OptimizeFor.Performance)]
    public struct PathJob : IJobFor
    {
        [NativeDisableParallelForRestriction]
        public BlobAssetReference<PathChunkArray> PathChunks;

        [ReadOnly, NativeDisableContainerSafetyRestriction]
        public NativeHashMap<int2, int>.ReadOnly ChunkIndexToListIndex;

        public int StartX;
        public int StartY;
        public int QuadrantWidth;
        public int QuadrantHeight;

        [BurstCompile]
        public void Execute(int index)
        {
            NativeArray<PathIndex> neighbours = new NativeArray<PathIndex>(4, Allocator.Temp);

            for (int x = StartX; x < QuadrantWidth + StartX; x++)
            for (int y = StartY; y < QuadrantHeight + StartY; y++)
            {
                CalculatePathAtIndex(neighbours, ref PathChunks.Value.PathChunks[index], new int2(x, y));
            }

            neighbours.Dispose();
        }

        private void CalculatePathAtIndex(NativeArray<PathIndex> neighbours, ref PathChunk pathChunk, int2 index)
        {
            int combinedIndex = index.x + index.y * PathUtility.GRID_WIDTH;
            byte targetIndex = pathChunk.TargetIndexes[combinedIndex];

            switch (targetIndex)
            {
                // Handle common case (~99%)
                case 0:
                {
                    if (GetClosestNeighbour(neighbours, ref pathChunk, index, combinedIndex, out int dist, out int dirIdx))
                    {
                        pathChunk.Directions[combinedIndex] = PathUtility.GetDirection(PathUtility.NeighbourDirectionsCardinal[dirIdx]);
                        pathChunk.Distances[combinedIndex] = pathChunk.NotWalkableIndexes[combinedIndex] ? 1_000_000_000 : dist;
                    }

                    return;
                }
                // Handle barricade case (targetIndex == 255)
                case byte.MaxValue:
                {
                    if (GetClosestNeighbour(neighbours, ref pathChunk, index, combinedIndex, out int dist, out _))
                    {
                        pathChunk.Directions[combinedIndex] = byte.MaxValue;
                        pathChunk.Distances[combinedIndex] = dist;
                    }

                    return;
                }
                default:
                    // Handle building case (targetIndex 1-254)
                    pathChunk.Distances[combinedIndex] = targetIndex * 50;
                    pathChunk.Directions[combinedIndex] = byte.MaxValue;
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool GetClosestNeighbour(NativeArray<PathIndex> neighbours, ref PathChunk pathChunk, int2 gridIndex, int combinedGridIndex, out int shortestDistance, out int dirIndex)
        {
            int2 currentChunkIndex = pathChunk.ChunkIndex;
            GetNeighbours(currentChunkIndex, gridIndex, neighbours);

            shortestDistance = int.MaxValue;
            dirIndex = 0;
            for (int i = 0; i < 4; i++)
            {
                PathIndex neighbourIndex = neighbours[i];
                if (neighbourIndex.GridIndex.x == -1) continue;

                int combinedIndex = neighbourIndex.GridIndex.x + neighbourIndex.GridIndex.y * PathUtility.GRID_WIDTH;

                ref PathChunk neighbour = ref neighbourIndex.ChunkIndex.Equals(currentChunkIndex)
                    ? ref pathChunk
                    : ref PathChunks.Value.PathChunks[ChunkIndexToListIndex[neighbourIndex.ChunkIndex]];

                // Check if the edge is blocked
                int blockedCost = IsEdgeBlocked(ref pathChunk, combinedGridIndex, ref neighbour, combinedIndex, i)
                    ? 100_000
                    : 0;
                
                int manhattanDist = i % 2 == 0 ? 5 : 7;
                int indexOccupied = neighbour.IndexOccupied[combinedIndex] ? 2_000 : 0;
                int dist = neighbour.Distances[combinedIndex]
                           + neighbour.MovementCosts[combinedIndex] * manhattanDist
                           + neighbour.ExtraDistance[combinedIndex]
                           + indexOccupied
                           + blockedCost;

                if (dist >= shortestDistance) continue;

                shortestDistance = dist;
                dirIndex = i;
            }

            return shortestDistance < int.MaxValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsEdgeBlocked(ref PathChunk pathChunk, int combinedGridIndex, ref PathChunk neighbourChunk, int combinedNeighbourIndex, int directionIndex)
        {
            // PathUtility.NeighbourDirectionsCardinal - Right, Up, Left, Down
            // Is Opposite!

            return directionIndex switch
            {
                0 => neighbourChunk.WestEdgeBlocks[combinedNeighbourIndex], // Right
                1 => pathChunk.NorthEdgeBlocks[combinedGridIndex], // Up
                2 => pathChunk.WestEdgeBlocks[combinedGridIndex], // Left
                3 => neighbourChunk.NorthEdgeBlocks[combinedNeighbourIndex], // Down
                _ => throw new ArgumentOutOfRangeException(nameof(directionIndex), directionIndex, null)
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GetNeighbours(int2 chunkIndex, int2 gridIndex, NativeArray<PathIndex> array)
        {
            for (int i = 0; i < 4; i++)
            {
                int2 dir = PathUtility.NeighbourDirectionsCardinal[i];
                int2 neighbour = new int2(gridIndex.x + dir.x, gridIndex.y + dir.y);

                array[i] = neighbour switch
                {
                    { x: < 0 } => ChunkIndexToListIndex.ContainsKey(new int2(chunkIndex.x - 1, chunkIndex.y))
                        ? new PathIndex(new int2(chunkIndex.x - 1, chunkIndex.y), new int2(PathUtility.GRID_WIDTH - 1, neighbour.y))
                        : new PathIndex(default, -1),
                    { x: >= PathUtility.GRID_WIDTH } => ChunkIndexToListIndex.ContainsKey(new int2(chunkIndex.x + 1, chunkIndex.y))
                        ? new PathIndex(new int2(chunkIndex.x + 1, chunkIndex.y), new int2(0, neighbour.y))
                        : new PathIndex(default, -1),
                    { y: < 0 } => ChunkIndexToListIndex.ContainsKey(new int2(chunkIndex.x, chunkIndex.y - 1))
                        ? new PathIndex(new int2(chunkIndex.x, chunkIndex.y - 1), new int2(neighbour.x, PathUtility.GRID_WIDTH - 1))
                        : new PathIndex(default, -1),
                    { y: >= PathUtility.GRID_WIDTH } => ChunkIndexToListIndex.ContainsKey(new int2(chunkIndex.x, chunkIndex.y + 1))
                        ? new PathIndex(new int2(chunkIndex.x, chunkIndex.y + 1), new int2(neighbour.x, 0))
                        : new PathIndex(default, -1),
                    _ => new PathIndex(chunkIndex, neighbour),
                };
            }
        }
    }
}
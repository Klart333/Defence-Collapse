using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using Pathfinding;
using Unity.Burst;

[BurstCompile(FloatPrecision.Low, FloatMode.Fast, OptimizeFor = OptimizeFor.Performance)]
public struct PathJob : IJobParallelForBatch
{
    [NativeDisableParallelForRestriction]
    public NativeArray<byte> Directions;
    [NativeDisableParallelForRestriction]
    public NativeArray<int> Distances;

    [ReadOnly]
    public NativeArray<bool>.ReadOnly NotWalkableIndexes;
    
    [ReadOnly]
    public NativeArray<bool>.ReadOnly TargetIndexes;
    
    [ReadOnly]
    public NativeArray<short>.ReadOnly MovementCosts;
    
    public int GridWidth;
    public int ArrayLength;
    public int BatchSize; 
    
    [BurstCompile]
    public void Execute(int startIndex, int count)
    {
        NativeArray<int> neighbours = new NativeArray<int>(8, Allocator.Temp);

        for (int i = startIndex; i < startIndex + count; i++)
        {
            if (TargetIndexes[i])
            {
                Distances[i] = 0;
                Directions[i] = byte.MaxValue;
                continue;
            }

            if (NotWalkableIndexes[i])
            {
                Distances[i] = int.MaxValue;
                continue;
            }

            GetNeighbours(i, neighbours);
            int shortestDistance = int.MaxValue;
            int dirIndex = 0;
            for (int j = 0; j < 8; j++)
            {
                int neighbourIndex = neighbours[j];
                if (neighbourIndex == -1) continue;

                int manhattanDist = j % 2 == 0 ? 10 : 14;
                int dist = Distances[neighbourIndex] + MovementCosts[neighbourIndex] * manhattanDist;
                if (dist >= shortestDistance) continue;
                
                shortestDistance = dist;
                dirIndex = j;
            }
            
            Distances[i] = shortestDistance;
            Directions[i] = GetDirection(PathManager.NeighbourDirections[dirIndex]);
        }

        neighbours.Dispose();
    }

    private void GetNeighbours(int index, NativeArray<int> array)
    {
        int x = index % GridWidth;
        int y = index / GridWidth;

        for (int i = 0; i < 8; i++)
        {
            int nx = x + PathManager.NeighbourDirections[i].x;
            int ny = y + PathManager.NeighbourDirections[i].y;

            if (nx >= 0 && nx < GridWidth && ny >= 0 && ny < ArrayLength / GridWidth)
            {
                array[i] = ny * GridWidth + nx;
            }
            else
            {
                array[i] = -1;
            }
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
using Unity.Mathematics;
using UnityEngine;

public static class ArrayExtensions
{
    public static bool IsInBounds<T>(this T[,,] array, int x, int y, int z)
    {
        if (x < 0 || x >= array.GetLength(0))
            return false;
        if (y < 0 || y >= array.GetLength(1))
            return false;
        if (z < 0 || z >= array.GetLength(2))
            return false;

        return true;
    }

    public static bool IsInBounds<T>(this T[,,] array, Vector3Int index)
    {
        return array.IsInBounds(index.x, index.y, index.z);
    }

    public static bool IsInBounds<T>(this T[,] array, int x, int y)
    {
        if (x < 0 || x >= array.GetLength(0))
            return false;
        if (y < 0 || y >= array.GetLength(1))
            return false;

        return true;
    }

    public static bool IsInBounds<T>(this T[,] array, Vector2Int index)
    {
        return array.IsInBounds(index.x, index.y);
    }
    
    
    public static bool IsInBounds<T>(this T[,] array, int2 index)
    {
        return array.IsInBounds(index.x, index.y);
    }
}
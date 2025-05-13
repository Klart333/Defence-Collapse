using Unity.Mathematics;
using UnityEngine;

public static class Extensions
{
    public static Vector2 XZ(this Vector3 v)
    {
        return new Vector2(v.x, v.z);
    }
    
    public static float3 XyZ(this float3 v, float y = 0)
    {
        return new float3(v.x, y, v.z);
    }
    
    public static Vector3 XyZ(this Vector3 v, float y = 0)
    {
        return new Vector3(v.x, y, v.z);
    }
    
    public static float3 XyZ(this int2 v, float y = 0)
    {
        return new float3(v.x, y, v.y);
    }
    
    public static int3 XyZ(this int2 v, int y = 0)
    {
        return new int3(v.x, y, v.y);
    }
    
    public static Vector3 ToVector3(this Vector2Int v)
    {
        return new Vector3(v.x, 0, v.y);
    }

    public static Vector2Int ToVector2Int(this Vector3 v)
    {
        return new Vector2Int((int)v.x, (int)v.z);
    }
    
    public static Vector2Int ToVector2Int(this int2 v)
    {
        return new Vector2Int(v.x, v.y);
    }
    
    public static Vector3 ToXyZ(this Vector2 v, float y = 0)
    {
        return new Vector3(v.x, y, v.y);
    }
    
    public static Vector3 MultiplyByAxis(this Vector3 a, Vector3 b)
    {
        return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
    }
    
    public static Vector3 DivideByAxis(this Vector3 a, Vector3 b)
    {
        return new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);
    }
    
    public static Vector3 MultiplyByAxis(this Vector3 a, int3 b)
    {
        return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
    }
}

public static class BoundsExtensions
{
    /// <summary>
    /// Converts Unity's Bounds to an ECS AABB (Axis-Aligned Bounding Box).
    /// </summary>
    public static AABB ToAABB(this Bounds bounds)
    {
        return new AABB
        {
            Center = bounds.center,
            Extents = bounds.extents
        };
    }
}
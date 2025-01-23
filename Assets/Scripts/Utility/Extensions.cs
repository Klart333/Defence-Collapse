using UnityEngine;

public static class Extensions
{
    public static Vector2 XZ(this Vector3 v)
    {
        return new Vector2(v.x, v.z);
    }

    public static Vector3 ToXyZ(this Vector2 v, float y = 0)
    {
        return new Vector3(v.x, y, v.y);
    }
}
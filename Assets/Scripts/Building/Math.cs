using UnityEngine;

public static class Math
{
    public static float Round(float x, float multiple)
    {
        float below = multiple * Mathf.FloorToInt(x / multiple);
        float above = multiple * Mathf.CeilToInt(x / multiple);
        if (x - below < above - x)
        {
            return below;
        }
        return above;
    }

    public static int GetMultiple(float x, float multiple)
    {
        return Mathf.RoundToInt(x / multiple);
    }
}
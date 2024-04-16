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

    public static float Elastic(float t)
    {
        float c4 = (2.0f * Mathf.PI) / 3.0f;

        return t == 0
          ? 0
          : t == 1
          ? 1
          : Mathf.Pow(1.5f, -10.0f * t) * Mathf.Sin((t * 7.5f - 0.75f) * c4) + 1.0f;
    }

    public static float InOutElastic(float t)
    {
        var c5 = (2f * Mathf.PI) / 4.5f;

        return t == 0f
          ? 0
          : t == 1f
          ? 1
          : t < 0.5f
          ? -(Mathf.Pow(1.5f, 10f * t - 10f) * Mathf.Sin((20f * t - 11.125f) * c5)) / 2f
          : (Mathf.Pow(1.5f, -10f * t + 10f) * Mathf.Sin((20f * t - 11.125f) * c5)) / 2f + 1f;
    }
    
    public static float EasInElastic(float t)
    {
        var c4 = (2f * Mathf.PI) / 3.0f;

        return t == 0
          ? 0
          : t == 1
          ? 1
          : -Mathf.Pow(2f, 10f * t - 10f) * Mathf.Sin((t * 10 - 10.75f) * c4);
    }
}
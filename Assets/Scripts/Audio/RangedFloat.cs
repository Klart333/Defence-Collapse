using System;

[Serializable]
public struct RangedFloat
{
    public float minValue;
    public float maxValue;

    public RangedFloat(float minValue, float maxValue)
    {
        this.maxValue = maxValue;
        this.minValue = minValue;
    }

    public readonly float Random() => UnityEngine.Random.Range(minValue, maxValue);  
}

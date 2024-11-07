using System;

[Serializable]
public class MinMaxRangeAttribute : Attribute // A attribute is for example [HideInInspector] or [SerializeField]
{
    public MinMaxRangeAttribute(float min, float max)
    {
        Min = min;
        Max = max;
    }

    public float Min { get; private set; }
    public float Max { get; private set; }
}

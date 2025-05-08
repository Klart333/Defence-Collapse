using UnityEngine;
using UnityEngine.VFX;

[VFXType(VFXTypeAttribute.Usage.GraphicsBuffer)]
public struct VFXHitSparksRequest
{
    public Vector3 Position;
    public Vector3 Color;
}
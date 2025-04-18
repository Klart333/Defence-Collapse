using Pathfinding;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct SpeedComponent : IComponentData
{
    public float Speed;
}

public struct FlowFieldComponent : IComponentData
{
    public PathIndex PathIndex;
    public float TurnSpeed;
    public float3 Forward;
    public float3 Up;
    public float3 TargetUp;    
    public LayerMask LayerMask;

    public float UpdateTimer;
}

public struct AttackSpeedComponent : IComponentData
{
    public float AttackSpeed;
    public float Timer;
}

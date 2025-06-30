using Unity.Mathematics;
using Unity.Rendering;
using Unity.Entities;
using Pathfinding;

namespace Enemy.ECS
{
    public struct SpeedComponent : IComponentData
    {
        public float Speed;
    }

    public struct FlowFieldComponent : IComponentData
    {
        public PathIndex PathIndex;
        public float TurnSpeed;
        public float3 TargetUp;
        public float3 Forward;
        public float3 Up;

        public int Importance;
    }

    public struct AttackSpeedComponent : IComponentData
    {
        public float AttackSpeed;
        public float Timer;
    }

    [MaterialProperty("_Strength")]
    public struct FresnelComponent : IComponentData
    {
        public float Value;
    }

}
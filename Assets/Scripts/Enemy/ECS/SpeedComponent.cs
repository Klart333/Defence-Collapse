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
        public int MoveTimer;
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
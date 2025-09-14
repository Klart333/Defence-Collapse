using Unity.Mathematics;
using Unity.Entities;
using Pathfinding;

namespace Enemy.ECS
{
    public struct ManagedEntityBuffer : IBufferElementData
    {
        public Entity Entity;
    }
    
    public struct EnemyClusterComponent : IComponentData
    {
        public PathIndex TargetPathIndex;
        public float3 Position;
        public float2 Facing;
        
        public float EnemySize;
        public int EnemyType;
    }
}
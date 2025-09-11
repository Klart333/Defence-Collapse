using Unity.Mathematics;
using Unity.Entities;

namespace Enemy.ECS
{
    public struct ManagedEntityBuffer : IBufferElementData
    {
        public Entity Entity;
    }
    
    public struct EnemyClusterComponent : IComponentData
    {
        public float2 Facing;
        public float3 Position;
        public float EnemySize;
        public int EnemyType;
    }
}
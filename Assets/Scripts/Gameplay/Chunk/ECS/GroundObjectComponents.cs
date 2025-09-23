using Pathfinding;
using Unity.Entities;

namespace Gameplay.Chunk.ECS
{
    public struct GroundObjectComponent : IComponentData
    {
        public PathIndex PathIndex;
    }
}
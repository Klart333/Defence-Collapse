using Unity.Entities;
using Unity.Transforms;

namespace Enemy.ECS
{
    public readonly partial struct EnemyTargetAspect : IAspect
    {
        public readonly RefRO<DirectionRangeComponent> DirectionComponent;
        public readonly RefRW<EnemyTargetComponent> EnemyTargetComponent;
        public readonly RefRO<RangeComponent> RangeComponent;
        public readonly RefRO<LocalTransform> LocalTransform;
    }
}
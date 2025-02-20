using Unity.Entities;

namespace Effects.ECS
{
    public readonly partial struct ColliderAspect : IAspect
    {
        public readonly RefRW<ColliderComponent> ColliderComponent;
        public readonly RefRW<DamageComponent> DamageComponent;
        public readonly RefRO<PositionComponent> PositionComponent;
    }
}
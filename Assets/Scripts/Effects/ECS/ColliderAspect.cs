using Unity.Entities;

namespace Effects.ECS
{
    public readonly partial struct ColliderAspect : IAspect
    {
        public readonly RefRW<ColliderComponent> ColliderComponent;
        public readonly RefRW<DamageComponent> DamageComponent;
        public readonly RefRW<RandomComponent> RandomComponent;
        
        public readonly RefRO<PositionComponent> PositionComponent;
        public readonly RefRO<CritComponent> CritComponent;
    }
}
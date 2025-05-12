using Effects.ECS;
using Unity.Entities;

namespace DataStructures.Queue.ECS
{
    public readonly partial struct AttackingAspect : IAspect
    {
        public readonly RefRW<AttackSpeedComponent> AttackSpeedComponent;
        public readonly RefRO<SimpleDamageComponent> DamageComponent;
        public readonly RefRO<AttackingComponent> AttackingComponent;

        public bool CanAttack(float deltaTime)
        {
            AttackSpeedComponent.ValueRW.Timer += deltaTime;
            return CanAttack();
        }

        public bool CanAttack() => AttackSpeedComponent.ValueRO.Timer > AttackSpeedComponent.ValueRO.AttackSpeed;
    }
}
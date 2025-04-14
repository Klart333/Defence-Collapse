using Unity.Entities;
using Unity.Transforms;

namespace DataStructures.Queue.ECS
{
    public readonly partial struct EnemyTargetAspect : IAspect
    {
        public readonly RefRW<EnemyTargetComponent> EnemyTargetComponent;
        public readonly RefRW<AttackSpeedComponent> AttackSpeedComponent;
        public readonly RefRO<RangeComponent> RangeComponent;
        public readonly RefRO<LocalTransform> LocalTransform;
        
        public bool ShouldFindTarget(float deltaTime)
        {
            AttackSpeedComponent.ValueRW.Timer += deltaTime;
            float delta = AttackSpeedComponent.ValueRO.AttackSpeed - AttackSpeedComponent.ValueRO.Timer;

            return delta < 0.1f;
        }

        public bool CanAttack()
        {
            return AttackSpeedComponent.ValueRO.Timer >= AttackSpeedComponent.ValueRO.AttackSpeed;
        }

        public void RestTimer()
        {
            AttackSpeedComponent.ValueRW.Timer = 0;
        }
    }
}
using Unity.Entities;
using Unity.Transforms;

namespace DataStructures.Queue.ECS
{
    public readonly partial struct EnemyTargetAspect : IAspect
    {
        public readonly RefRO<DirectionRangeComponent> DirectionComponent;
        public readonly RefRW<EnemyTargetComponent> EnemyTargetComponent;
        public readonly RefRO<RangeComponent> RangeComponent;
        public readonly RefRO<LocalTransform> LocalTransform;
        
        private readonly RefRW<AttackSpeedComponent> AttackSpeedComponent;
        
        public bool ShouldFindTarget(float deltaTime)
        {
            AttackSpeedComponent.ValueRW.Timer += deltaTime;
            float delta = AttackSpeedComponent.ValueRO.AttackSpeed - AttackSpeedComponent.ValueRO.Timer;

            return delta < 0.2f;
        }

        public bool CanAttack()
        {
            return EnemyTargetComponent.ValueRO.HasTarget 
                   && AttackSpeedComponent.ValueRO.Timer >= AttackSpeedComponent.ValueRO.AttackSpeed;
        }

        public void RestTimer()
        {
            AttackSpeedComponent.ValueRW.Timer = 0;
            EnemyTargetComponent.ValueRW.HasTarget = false;
        }
    }
}
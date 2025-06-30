using Enemy.ECS;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace Effects.LittleDudes
{
    public partial struct LittleDudeAttackingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
               
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }

    [BurstCompile, WithAll(typeof(LittleDudeComponent))]
    public partial struct LittleDudeAttackingJob : IJobEntity
    {
        public float DeltaTime;

        public EntityCommandBuffer.ParallelWriter ECB;
        
        public void Execute(ref AttackSpeedComponent attackSpeed, in LocalTransform transform)
        {
            attackSpeed.Timer += DeltaTime;

            if (attackSpeed.Timer >= attackSpeed.AttackSpeed)
            {
                
            }
        }
    }

    public struct LittleDudeAttackingComponent : IComponentData
    {
        public float Timer;
    }
}
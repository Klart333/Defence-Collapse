using Unity.Entities;
using Effects.ECS;
using Unity.Burst;

namespace Buildings.District.ECS
{
    [BurstCompile, UpdateAfter(typeof(DeathSystem))]
    public partial struct AnimateBowSystem : ISystem
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

    [BurstCompile]
    public partial struct AnimateBowJob : IJobEntity
    {
        public void Execute()
        {
            
        }
    }
}
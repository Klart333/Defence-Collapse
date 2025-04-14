using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace Effects.ECS
{
    public partial struct PositionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new PositionJob().ScheduleParallel();
            
            state.CompleteDependency();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }

    [BurstCompile]
    public partial struct PositionJob : IJobEntity
    {
        [BurstCompile]
        public void Execute(in PositionComponent position, ref LocalTransform transform)
        {
            transform.Position = position.Position;
        }
    }
}
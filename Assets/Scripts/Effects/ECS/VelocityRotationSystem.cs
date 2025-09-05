using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Effects.ECS
{
    [BurstCompile, UpdateAfter(typeof(DeathSystem))]
    public partial struct VelocityRotationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new VelocityRotationJob().ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }

    [BurstCompile]
    public partial struct VelocityRotationJob : IJobEntity
    {
        [BurstCompile]
        public void Execute(ref RotateTowardsVelocityComponent rot, ref LocalTransform transform)
        {
            float3 direction = math.normalize(transform.Position - rot.LastPosition);
            rot.LastPosition = transform.Position;

            transform.Rotation = quaternion.LookRotation(direction, new float3(0, 1, 0));
        }
    }
}
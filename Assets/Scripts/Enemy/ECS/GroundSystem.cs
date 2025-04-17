using Unity.Physics.Systems;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Entities;
using Unity.Physics;
using Unity.Burst;

namespace DataStructures.Queue.ECS
{
    [UpdateInGroup(typeof(PhysicsSimulationGroup))]
    public partial struct GroundSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsWorldSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            CollisionWorld collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;

            new GroundRayJob()
            {
                CollisionWorld = collisionWorld,
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }

    [BurstCompile]
    public partial struct GroundRayJob: IJobEntity
    {
        [ReadOnly]
        public CollisionWorld CollisionWorld;
        
        [BurstCompile]
        public void Execute(ref LocalTransform transform, ref FlowFieldComponent flowFieldComponent)
        {
            RaycastInput input = new RaycastInput()
            {
                Start = transform.Position + new float3(0, 1, 0),
                End = transform.Position.XyZ(-0.5f),
                Filter = new CollisionFilter()
                {
                    BelongsTo = 6,
                    CollidesWith = 6, 
                    GroupIndex = 0
                }
            };

            if (!CollisionWorld.CastRay(input, out RaycastHit hit)) return;
            
            transform.Position = hit.Position;
            flowFieldComponent.TargetUp = hit.SurfaceNormal;
        }
    }
}
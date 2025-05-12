using System.Numerics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace InputCamera.ECS
{
    public partial struct RotateTowardCameraSystem : ISystem
    {
        private ComponentLookup<LocalToWorld> transformLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CameraPositionComponent>();
            transformLookup = state.GetComponentLookup<LocalToWorld>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float3 camPos = SystemAPI.GetSingleton<CameraPositionComponent>().Position;

            state.Dependency = new RotationJob
            {
                CameraPosition = camPos,
            }.ScheduleParallel(state.Dependency);
           
            transformLookup.Update(ref state);
            state.Dependency = new RotationLTWJob
            {
                CameraPosition = camPos,
                TransformLookup = transformLookup,
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
    
    [BurstCompile, WithAll(typeof(RotateTowardCameraTag))]
    public partial struct RotationJob : IJobEntity
    {
        public float3 CameraPosition;
        
        [BurstCompile]
        public void Execute(ref LocalTransform transform)
        {
            float3 dir = math.normalize(transform.Position - CameraPosition);
            transform.Rotation = quaternion.LookRotation(dir, transform.Up());
        }
    }
    
    [BurstCompile, WithAll(typeof(RotateTowardCameraLTWTag))]
    public partial struct RotationLTWJob : IJobEntity // IF BOTTLENECK CHANGE TO FOLLOW POSITION WITHOUT PARENT / CHILD
    {
        public float3 CameraPosition;
        
        [ReadOnly]
        public ComponentLookup<LocalToWorld> TransformLookup;
        
        [BurstCompile]
        public void Execute(ref LocalTransform transform, in Parent parent)
        {
            LocalToWorld parentTransform = TransformLookup[parent.Value];
            float3 dir = math.normalize(parentTransform.Position - CameraPosition);
            quaternion rot = quaternion.LookRotation(dir, parentTransform.Up);
            transform.Rotation = math.mul(math.inverse(parentTransform.Rotation), rot);
        }
    }
}
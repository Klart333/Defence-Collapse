using Enemy.ECS;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Buildings.District.ECS
{
    public partial struct DistrictTargetMeshSystem : ISystem
    {
        private ComponentLookup<EnemyTargetComponent> targetLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            targetLookup = SystemAPI.GetComponentLookup<EnemyTargetComponent>();
            
            state.RequireForUpdate<FlowFieldComponent>(); // Require Enemy
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            targetLookup.Update(ref state);

            new RotateTargetMeshJob
            {
                TargetLookup = targetLookup,
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }

    [BurstCompile]
    public partial struct RotateTargetMeshJob : IJobEntity
    {
        [ReadOnly]
        public ComponentLookup<EnemyTargetComponent> TargetLookup;
        
        public void Execute(in TargetMeshComponent targetMesh, ref LocalTransform transform)
        {
            if (!TargetLookup.TryGetComponent(targetMesh.Target, out EnemyTargetComponent target) || !target.HasTarget)
            {
                return;
            }
            
            float3 dir = math.normalize(target.TargetPosition - transform.Position); 
            transform.Rotation = quaternion.LookRotation(dir, new float3(0, 1, 0));
        }
    }
}
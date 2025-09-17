using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.Entities;
using Unity.Burst;
using Enemy.ECS;

namespace Buildings.District.ECS
{
    public partial struct DistrictAttachementMeshSystem : ISystem
    {
        private ComponentLookup<EnemyTargetComponent> targetLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state) 
        {
            targetLookup = SystemAPI.GetComponentLookup<EnemyTargetComponent>(true); 
            
            state.RequireForUpdate<TargetingActivationComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            targetLookup.Update(ref state);
 
            new RotateAttachementMeshJob
            {
                TargetLookup = targetLookup,
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }

    [BurstCompile, WithAll(typeof(TargetingActivationComponent))]
    public partial struct RotateAttachementMeshJob : IJobEntity
    {
        [ReadOnly]
        public ComponentLookup<EnemyTargetComponent> TargetLookup;
        
        public void Execute(in AttachementMeshComponent attachementMesh, ref LocalTransform transform)
        {
            if (!TargetLookup.TryGetComponent(attachementMesh.Target, out EnemyTargetComponent target) || !target.HasTarget)
            {
                return;
            }
            
            float3 dir = math.normalize(target.TargetPosition - transform.Position); 
            transform.Rotation = quaternion.LookRotation(dir, new float3(0, 1, 0));
        }
    }
}
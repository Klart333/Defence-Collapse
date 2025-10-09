using Buildings.District.DistrictAttachment;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.Entities;
using Unity.Burst;
using Enemy.ECS;

namespace Buildings.District.ECS
{
    [BurstCompile, UpdateAfter(typeof(ClosestTargetingSystem))]
    public partial struct DistrictAttachementMeshSystem : ISystem
    {
        private ComponentLookup<EnemyTargetComponent> targetLookup;
        private ComponentLookup<AttackSpeedComponent> speedLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state) 
        {
            targetLookup = SystemAPI.GetComponentLookup<EnemyTargetComponent>(true);
            speedLookup = SystemAPI.GetComponentLookup<AttackSpeedComponent>(true);
            
            state.RequireForUpdate<UpdateTargetingTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            targetLookup.Update(ref state);
            speedLookup.Update(ref state);
                
            new RotateAttachementMeshJob
            {
                AttackSpeedLookup = speedLookup,
                TargetLookup = targetLookup,
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }

    [BurstCompile]
    public partial struct RotateAttachementMeshJob : IJobEntity
    {
        [ReadOnly]
        public ComponentLookup<EnemyTargetComponent> TargetLookup;

        [ReadOnly]
        public ComponentLookup<AttackSpeedComponent> AttackSpeedLookup;
        
        public void Execute(in AttachementMeshComponent attachementMesh, ref AttachmentAttackValue attachmentAttackValue, ref LocalTransform transform)
        {
            if (!TargetLookup.TryGetComponent(attachementMesh.Target, out EnemyTargetComponent target) || !target.HasTarget)
            {
                return;
            }
            
            float3 dir = math.normalize(target.TargetPosition.xz - transform.Position.xz).XyZ(); 
            transform.Rotation = quaternion.LookRotation(dir, new float3(0, 1, 0));

            if (AttackSpeedLookup.TryGetComponent(attachementMesh.Target, out AttackSpeedComponent attackSpeed))
            {
                attachmentAttackValue.Value = 1.0f - (attackSpeed.AttackTimer / attackSpeed.AttackSpeed);
            }
        }
    }
}
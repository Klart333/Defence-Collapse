using Unity.Collections;
using Unity.Transforms;
using Unity.Entities;
using Unity.Burst;
using Enemy.ECS;

namespace Buildings.District.ECS
{
    [BurstCompile, UpdateAfter(typeof(ClosestTargetingSystem))]
    public partial struct DistrictEntityShootSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TargetingActivationComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
            
            state.Dependency = new UpdateTargetedDistrictEntitiesJob
            {
                ECB = ecb.AsParallelWriter(),
            }.ScheduleParallel(state.Dependency);
            
            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
    
    [BurstCompile]
    public partial struct UpdateTargetedDistrictEntitiesJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        
        public void Execute([ChunkIndexInQuery]int sortKey, Entity entity, ref EnemyTargetComponent targetComponent, ref TargetingActivationComponent targetingActivationComponent, in LocalTransform transform, in DistrictDataComponent districtData)
        {
            if (!targetComponent.HasTarget)
            {
                ECB.RemoveComponent<TargetingActivationComponent>(sortKey, entity);
                return;
            }

            Entity dataEntity = ECB.CreateEntity(sortKey);
            ECB.AddComponent(sortKey, dataEntity, new DistrictEntityData
            {
                TargetPosition = targetComponent.TargetPosition,
                DistrictID = districtData.DistrictID,
                OriginPosition = transform.Position
            });

            targetComponent.HasTarget = false;
            targetingActivationComponent.Count--;
            if (targetingActivationComponent.Count <= 0)
            {
                ECB.RemoveComponent<TargetingActivationComponent>(sortKey, entity);
            }
        }
    }
}
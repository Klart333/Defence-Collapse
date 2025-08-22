using Unity.Collections;
using Unity.Transforms;
using Unity.Entities;
using Unity.Burst;
using Enemy.ECS;
using Gameplay;

namespace Buildings.District.ECS
{
    public partial struct UpdateDistrictEntitiesSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameSpeedComponent>();
            state.RequireForUpdate<FlowFieldComponent>(); // Require Enemy
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
            float gameSpeed = SystemAPI.GetSingleton<GameSpeedComponent>().Speed;
            float deltaTime = SystemAPI.Time.DeltaTime * gameSpeed;

            state.Dependency = new UpdateTargetedDistrictEntitiesJob 
            {  
                ECB = ecb.AsParallelWriter(),
                DeltaTime = deltaTime
            }.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();

            state.Dependency = new UpdateSimpleDistrictEntitiesJob
            {
                ECB = ecb.AsParallelWriter(),
                DeltaTime = deltaTime
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
        public float DeltaTime;
        
        public void Execute([ChunkIndexInQuery]int sortKey, ref AttackSpeedComponent attackSpeedComponent, ref EnemyTargetComponent targetComponent, in LocalTransform transform, in DistrictDataComponent districtData)
        {
            attackSpeedComponent.Timer += DeltaTime;
            if (attackSpeedComponent.Timer < attackSpeedComponent.AttackSpeed
                || !targetComponent.HasTarget) return;

            attackSpeedComponent.Timer = 0;
            targetComponent.HasTarget = false;

            Entity entity = ECB.CreateEntity(sortKey);
            ECB.AddComponent(sortKey, entity, new DistrictEntityData
            {
                DistrictID = districtData.DistrictID,
                TargetPosition = targetComponent.TargetPosition,
                OriginPosition = transform.Position
            });
        }
    }
    
    [BurstCompile, WithNone(typeof(EnemyTargetComponent))]
    public partial struct UpdateSimpleDistrictEntitiesJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        public float DeltaTime;

        public void Execute([ChunkIndexInQuery]int sortKey, ref AttackSpeedComponent attackSpeedComponent, in LocalTransform transform, in DistrictDataComponent districtData)
        {
            attackSpeedComponent.Timer += DeltaTime;
            if (attackSpeedComponent.Timer < attackSpeedComponent.AttackSpeed) return;

            attackSpeedComponent.Timer = 0;
            
            Entity entity = ECB.CreateEntity(sortKey);
            ECB.AddComponent(sortKey, entity, new DistrictEntityData
            {
                DistrictID = districtData.DistrictID,
                OriginPosition = transform.Position
            });
        }
    }
}
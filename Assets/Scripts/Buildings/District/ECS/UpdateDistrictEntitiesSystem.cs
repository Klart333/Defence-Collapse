using Effects.ECS;
using Enemy.ECS;
using Gameplay.Turns.ECS;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Buildings.District.ECS
{
    [UpdateAfter(typeof(DeathSystem))]
    public partial struct UpdateDistrictEntitiesSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TurnIncreaseComponent>();
            state.RequireForUpdate<FlowFieldComponent>(); // Require Enemy
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            TurnIncreaseComponent turnIncrease = SystemAPI.GetSingleton<TurnIncreaseComponent>();
            
            EntityCommandBuffer ecb1 = new EntityCommandBuffer(Allocator.TempJob);
            EntityCommandBuffer ecb2 = new EntityCommandBuffer(Allocator.TempJob);

            state.Dependency = new UpdateTargetedDistrictEntitiesJob 
            {  
                ECB = ecb1.AsParallelWriter(),
                TurnIncrease = turnIncrease.TurnIncrease,
            }.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();

            state.Dependency = new UpdateSimpleDistrictEntitiesJob
            {
                ECB = ecb2.AsParallelWriter(),
                TurnIncrease = turnIncrease.TurnIncrease,
            }.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();
            
            ecb1.Playback(state.EntityManager);
            ecb1.Dispose();
            
            ecb2.Playback(state.EntityManager);
            ecb2.Dispose();
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
        
        public int TurnIncrease;
        
        public void Execute([ChunkIndexInQuery]int sortKey, ref AttackSpeedComponent attackSpeedComponent, ref EnemyTargetComponent targetComponent, in LocalTransform transform, in DistrictDataComponent districtData)
        {
            if (!targetComponent.HasTarget)
            {
                attackSpeedComponent.AttackTimer = math.max(attackSpeedComponent.AttackTimer - TurnIncrease, 0);
                return;
            }
            
            attackSpeedComponent.AttackTimer -= TurnIncrease;
            if (attackSpeedComponent.AttackTimer > 0) return;
            
            attackSpeedComponent.AttackTimer += attackSpeedComponent.AttackSpeed;
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
        public int TurnIncrease;

        public void Execute([ChunkIndexInQuery]int sortKey, ref AttackSpeedComponent attackSpeedComponent, in LocalTransform transform, in DistrictDataComponent districtData)
        {
            attackSpeedComponent.AttackTimer -= TurnIncrease;
            if (attackSpeedComponent.AttackTimer > 0) return;
            
            attackSpeedComponent.AttackTimer += attackSpeedComponent.AttackSpeed;
            
            Entity entity = ECB.CreateEntity(sortKey);
            ECB.AddComponent(sortKey, entity, new DistrictEntityData
            {
                DistrictID = districtData.DistrictID,
                OriginPosition = transform.Position
            });
        }
    }
}
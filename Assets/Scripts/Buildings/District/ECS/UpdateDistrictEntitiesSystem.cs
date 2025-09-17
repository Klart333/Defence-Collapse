using Gameplay.Turns.ECS;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Effects.ECS;
using Enemy.ECS;
using UnityEngine;

namespace Buildings.District.ECS
{
    [UpdateAfter(typeof(DeathSystem)), UpdateBefore(typeof(ClosestTargetingSystem))]
    public partial struct UpdateDistrictEntitiesSystem : ISystem
    {
        private EntityQuery updateDistrictQuery;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            updateDistrictQuery = SystemAPI.QueryBuilder().WithAll<TurnIncreaseComponent, UpdateDistrictTag>().Build();
            
            state.RequireForUpdate<UpdateDistrictTag>();
            state.RequireForUpdate<EnemyClusterComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            TurnIncreaseComponent turnIncrease = updateDistrictQuery.GetSingleton<TurnIncreaseComponent>();
            
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

            state.Dependency = new UpdateDistrictEntitiesJob 
            {  
                ECB = ecb.AsParallelWriter(),
                TurnIncrease = turnIncrease.TurnIncrease,
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
    
    // 1. UpdateTargetedDistrictEntitiesJob can shoot? Yes -> add component with count
    // 2. ClosestTargetJob all with tag, get closet enemy
    // 3. FireCallbackDistrictSystem all with tag, has enemy? Yes -> Send data. count-- if 0 remove component

    [BurstCompile, WithAll(typeof(DistrictDataComponent))]
    public partial struct UpdateDistrictEntitiesJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        
        public int TurnIncrease;
        
        public void Execute([ChunkIndexInQuery]int sortKey, Entity entity, ref AttackSpeedComponent attackSpeedComponent)
        {
            attackSpeedComponent.AttackTimer -= TurnIncrease;
            if (attackSpeedComponent.AttackTimer > 0) return;
            
            int count = math.max(1, (int)math.ceil(-attackSpeedComponent.AttackTimer / attackSpeedComponent.AttackSpeed));
            attackSpeedComponent.AttackTimer += attackSpeedComponent.AttackSpeed * count;

            ECB.AddComponent(sortKey, entity, new TargetingActivationComponent
            {
                Count = count,
            });
        }
    }
}
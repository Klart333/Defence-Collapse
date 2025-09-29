using Buildings.District.ECS;
using Unity.Entities;
using Unity.Burst;
using Effects.ECS;
using Enemy.ECS;

namespace Gameplay.Turns.ECS
{
    [BurstCompile, UpdateBefore(typeof(DeathSystem))]
    public partial struct TurnSystem : ISystem
    {
        private EntityQuery updateDistrictQuery;
        private EntityQuery updateEnemiesQuery;
        private EntityQuery blockerQuery;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TurnIncreaseComponent>();
            
            updateDistrictQuery = SystemAPI.QueryBuilder().WithAll<TurnIncreaseComponent>().WithNone<TurnProgressionComponent>().Build();
            updateEnemiesQuery = SystemAPI.QueryBuilder().WithAll<TurnIncreaseComponent, TurnProgressionComponent>().Build();
            blockerQuery = SystemAPI.QueryBuilder().WithAny<ProgressionBlockerTag, TargetingActivationComponent, MovingClusterComponent>().Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.TryGetSingletonEntity<UpdateDistrictTag>(out Entity updateDistrictEntity))
            {
                state.EntityManager.RemoveComponent<UpdateDistrictTag>(updateDistrictEntity);
                return;
            }
            
            if (SystemAPI.TryGetSingletonEntity<UpdateEnemiesTag>(out Entity updateEnemiesEntity))
            {
                state.EntityManager.RemoveComponent<UpdateEnemiesTag>(updateEnemiesEntity);
                return;
            }
            
            if (!updateDistrictQuery.IsEmpty)
            {
                UpdateDistricts(ref state);
                return;
            }

            if (!blockerQuery.IsEmpty)
            {
                return;
            }

            if (!updateEnemiesQuery.IsEmpty && UpdateEnemies(ref state))
            {
                return;
            }

            // Already updated enemies, and there are no blockers -> Remove Turn Entity
            Entity turnEntity = SystemAPI.GetSingletonEntity<TurnIncreaseComponent>();
            state.EntityManager.AddComponent<DeathTag>(turnEntity);
        }

        private void UpdateDistricts(ref SystemState state)
        {
            Entity turnEntity = updateDistrictQuery.GetSingletonEntity();
            state.EntityManager.AddComponentData(turnEntity, new UpdateDistrictTag());
            state.EntityManager.AddComponentData(turnEntity, new TurnProgressionComponent
            {
                UpdatedDistrict = true,
                UpdatedEnemies = false,
            });
        }
        
        private bool UpdateEnemies(ref SystemState state)
        {
            Entity turnEntity = updateEnemiesQuery.GetSingletonEntity();
            if (state.EntityManager.GetComponentData<TurnProgressionComponent>(turnEntity).UpdatedEnemies)
            {
                // Already Updated Enemies
                return false;
            }
            
            state.EntityManager.AddComponentData(turnEntity, new UpdateEnemiesTag());
            state.EntityManager.SetComponentData(turnEntity, new TurnProgressionComponent
            {
                UpdatedDistrict = true,
                UpdatedEnemies = true,
            });
            
            return true;
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}
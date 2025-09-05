using Effects.ECS;
using Unity.Burst;
using Unity.Entities;

namespace Gameplay.Turns.ECS
{
    [UpdateAfter(typeof(DeathSystem)), BurstCompile]
    public partial struct TurnSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TurnIncreaseComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            Entity turnEntity = SystemAPI.GetSingletonEntity<TurnIncreaseComponent>();
            state.EntityManager.AddComponent<DeathTag>(turnEntity);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}
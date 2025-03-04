using System.Collections.Generic;
using Unity.Entities;
using Unity.Burst;
using System;

namespace Effects.ECS
{
    public partial struct DeathSystem : ISystem
    {
        public static readonly Dictionary<int, Action> DeathCallbacks = new Dictionary<int, Action>();
        public static int Key = 0;
        
        private EntityQuery deathQuery;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            deathQuery = SystemAPI.QueryBuilder().WithAll<DeathTag>().Build();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (deathQuery.IsEmpty)
            {
                return;
            }

            foreach (RefRO<DeathCallbackComponent> callback in SystemAPI.Query<RefRO<DeathCallbackComponent>>().WithAll<DeathTag>() )
            {
                if (DeathCallbacks.TryGetValue(callback.ValueRO.Key, out Action action))
                {
                    action.Invoke();
                    DeathCallbacks.Remove(callback.ValueRO.Key);
                }
            }

            state.EntityManager.DestroyEntity(deathQuery.ToEntityArray(state.WorldUpdateAllocator));
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            
        }
    }
}
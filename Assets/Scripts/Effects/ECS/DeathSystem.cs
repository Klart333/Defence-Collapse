using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using System;

namespace Effects.ECS
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial class DeathSystem : SystemBase
    {
        public static readonly Dictionary<int, Action> DeathCallbacks = new Dictionary<int, Action>();
        public static int Key = 0;

        private EntityQuery deathQuery;

        protected override void OnStartRunning()
        {
            base.OnStartRunning();

            deathQuery = SystemAPI.QueryBuilder().WithAll<DeathTag>().Build();
        }

        protected override void OnUpdate()
        {
            if (deathQuery.IsEmpty)
            {
                return;
            }

            foreach (RefRO<DeathCallbackComponent> callback in SystemAPI.Query<RefRO<DeathCallbackComponent>>().WithAll<DeathTag>())
            {
                if (DeathCallbacks.TryGetValue(callback.ValueRO.Key, out Action action))
                {
                    action.Invoke();
                    DeathCallbacks.Remove(callback.ValueRO.Key);
                }
            }

            NativeArray<Entity> deathEntities = deathQuery.ToEntityArray(Allocator.Temp);
            EntityManager.DestroyEntity(deathEntities);
            deathEntities.Dispose();
        }
    }
}
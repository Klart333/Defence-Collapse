using Unity.Mathematics;
using Unity.Collections;
using Effects.ECS.ECB;
using Unity.Entities;
using Unity.Burst;
using Gameplay;
using System;

namespace Effects.ECS
{
    [UpdateAfter(typeof(FireCollisionSystem)), UpdateBefore(typeof(HealthSystem))]
    public partial struct FireDamageSystem : ISystem
    {
        private ComponentLookup<PendingDamageComponent> pendingDamageLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameSpeedComponent>();
            state.RequireForUpdate<FireTickDataComponent>();
            state.RequireForUpdate<PendingDamageECBSystem.Singleton>();
            pendingDamageLookup = state.GetComponentLookup<PendingDamageComponent>(true);
            
            EntityQueryBuilder builder = new EntityQueryBuilder(state.WorldUpdateAllocator).WithAll<FireComponent, HealthComponent>();
            state.RequireForUpdate(state.GetEntityQuery(builder));
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            FireTickDataComponent fireTickData = SystemAPI.GetSingleton<FireTickDataComponent>();
            float gameSpeed = SystemAPI.GetSingleton<GameSpeedComponent>().Speed;
            
            PendingDamageECBSystem.Singleton singleton = SystemAPI.GetSingleton<PendingDamageECBSystem.Singleton>();
            EntityCommandBuffer ecb = singleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            pendingDamageLookup.Update(ref state);
            
            state.Dependency = new FireDamageJob
            {
                PendingDamageLookup = pendingDamageLookup,
                ECB = ecb.AsParallelWriter(), 
                TickDamage = fireTickData.TickDamage,
                TickRate = fireTickData.TickRate,
                DeltaTime = SystemAPI.Time.DeltaTime * gameSpeed,
            }.ScheduleParallel(state.Dependency);
            
            state.Dependency.Complete();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            
        }
    }
    
    [BurstCompile, WithAll(typeof(HealthComponent))]
    public partial struct FireDamageJob : IJobEntity
    {
        [ReadOnly]
        public ComponentLookup<PendingDamageComponent> PendingDamageLookup;
        
        public EntityCommandBuffer.ParallelWriter ECB;

        public float TickDamage;
        public float TickRate;
        public float DeltaTime;
        
        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, ref FireComponent fireComponent)
        {
            fireComponent.Timer += DeltaTime;
            if (fireComponent.Timer < TickRate) return;
            fireComponent.Timer = 0;
            
            float damage = math.min(fireComponent.TotalDamage, TickDamage);
            fireComponent.TotalDamage -= damage;

            if (!PendingDamageLookup.TryGetComponent(entity, out PendingDamageComponent pendingDamage))
            {
                pendingDamage = new PendingDamageComponent
                {
                    HealthDamage = damage,
                    ArmorDamage = damage,
                    ShieldDamage = damage,
                };
            }
            ECB.AddComponent(sortKey, entity, pendingDamage);

            if (fireComponent.TotalDamage <= 0)
            {
                ECB.RemoveComponent<FireComponent>(sortKey, entity);
            }
        }
    }

    [Serializable]
    public struct FireTickDataComponent : IComponentData
    {
        public float TickRate;
        public float TickDamage;
    }
}
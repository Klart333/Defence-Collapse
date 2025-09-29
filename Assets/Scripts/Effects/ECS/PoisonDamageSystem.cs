using Unity.Mathematics;
using Unity.Collections;
using Effects.ECS.ECB;
using Unity.Entities;
using Unity.Burst;
using Gameplay;
using System;

namespace Effects.ECS
{
    [UpdateAfter(typeof(PoisonCollisionSystem)), UpdateBefore(typeof(HealthSystem))]
    public partial struct PoisonDamageSystem : ISystem
    {
        private ComponentLookup<PendingDamageComponent> pendingDamageLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameSpeedComponent>();
            state.RequireForUpdate<PoisonTickDataComponent>();
            state.RequireForUpdate<PendingDamageECBSystem.Singleton>();
            pendingDamageLookup = state.GetComponentLookup<PendingDamageComponent>(true);
            
            EntityQueryBuilder builder = new EntityQueryBuilder(state.WorldUpdateAllocator).WithAll<PoisonComponent, HealthComponent>();
            state.RequireForUpdate(state.GetEntityQuery(builder));
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            PoisonTickDataComponent poisonTickData = SystemAPI.GetSingleton<PoisonTickDataComponent>();
            float gameSpeed = SystemAPI.GetSingleton<GameSpeedComponent>().Speed;
            
            PendingDamageECBSystem.Singleton singleton = SystemAPI.GetSingleton<PendingDamageECBSystem.Singleton>();
            EntityCommandBuffer ecb = singleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            pendingDamageLookup.Update(ref state);
            
            state.Dependency = new PoisonDamageJob
            {
                PendingDamageLookup = pendingDamageLookup,
                ECB = ecb.AsParallelWriter(), 
                TickDamage = poisonTickData.TickDamage,
                TickRate = poisonTickData.TickRate,
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
    public partial struct PoisonDamageJob : IJobEntity
    {
        [ReadOnly]
        public ComponentLookup<PendingDamageComponent> PendingDamageLookup;
        
        public EntityCommandBuffer.ParallelWriter ECB;

        public float TickDamage;
        public float TickRate;
        public float DeltaTime;
        
        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, ref PoisonComponent poisonComponent)
        {
            poisonComponent.Timer += DeltaTime;
            if (poisonComponent.Timer < TickRate) return;
            poisonComponent.Timer = 0;
            
            float damage = math.min(poisonComponent.TotalDamage, TickDamage);
            poisonComponent.TotalDamage -= damage;

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

            if (poisonComponent.TotalDamage <= 0)
            {
                ECB.RemoveComponent<PoisonComponent>(sortKey, entity);
            }
        }
    }

    [Serializable]
    public struct PoisonTickDataComponent : IComponentData
    {
        public float TickRate;
        public float TickDamage;
    }
}
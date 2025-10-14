using Unity.Mathematics;
using Effects.ECS.ECB;
using Unity.Entities;
using Unity.Burst;
using Enemy.ECS;
using Gameplay;
using System;

namespace Effects.ECS
{
    [BurstCompile, UpdateAfter(typeof(CollisionSystem)), UpdateBefore(typeof(BeforeDamageEffectsECBSystem))]
    public partial struct PoisonDamageSystem : ISystem 
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<PoisonComponent, HealthComponent>().Build());
            state.RequireForUpdate<BeforeHealthECBSystem.Singleton>();
            state.RequireForUpdate<PoisonTickDataComponent>();
            state.RequireForUpdate<GameSpeedComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            PoisonTickDataComponent poisonTickData = SystemAPI.GetSingleton<PoisonTickDataComponent>();
            float gameSpeed = SystemAPI.GetSingleton<GameSpeedComponent>().Speed;
            
            BeforeHealthECBSystem.Singleton singleton = SystemAPI.GetSingleton<BeforeHealthECBSystem.Singleton>();
            EntityCommandBuffer ecb = singleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            new PoisonDamageJob
            {
                ECB = ecb.AsParallelWriter(), 
                TickDamage = poisonTickData.TickDamage,
                TickRate = poisonTickData.TickRate,
                DeltaTime = SystemAPI.Time.DeltaTime * gameSpeed,
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            
        }
    }
    
    [BurstCompile, WithAll(typeof(HealthComponent))]
    public partial struct PoisonDamageJob : IJobEntity
    {
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

            ECB.AppendToBuffer(sortKey, entity, new DamageBuffer
            {
                Damage = damage,
                ArmorPenetration = GameDetailsData.PoisonArmorPenetration,
            });
            
            ECB.AddComponent<PendingDamageTag>(sortKey, entity);

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
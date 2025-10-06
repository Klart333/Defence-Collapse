using Unity.Mathematics;
using Unity.Collections;
using Effects.ECS.ECB;
using Unity.Entities;
using Unity.Burst;
using Gameplay;
using System;
using Enemy.ECS;

namespace Effects.ECS
{
    [BurstCompile, UpdateAfter(typeof(CollisionSystem)), UpdateBefore(typeof(BeforeDamageEffectsECBSystem))]
    public partial struct FireDamageSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeforeDamageEffectsECBSystem.Singleton>();
            state.RequireForUpdate<FireTickDataComponent>();
            state.RequireForUpdate<GameSpeedComponent>();
            
            state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<FireComponent, HealthComponent>().Build());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            FireTickDataComponent fireTickData = SystemAPI.GetSingleton<FireTickDataComponent>();
            float gameSpeed = SystemAPI.GetSingleton<GameSpeedComponent>().Speed;
            
            BeforeDamageEffectsECBSystem.Singleton singleton = SystemAPI.GetSingleton<BeforeDamageEffectsECBSystem.Singleton>();
            EntityCommandBuffer ecb = singleton.CreateCommandBuffer(state.WorldUnmanaged);

            new FireDamageJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime * gameSpeed,
                TickDamage = fireTickData.TickDamage,
                TickRate = fireTickData.TickRate,
                ECB = ecb.AsParallelWriter(),
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            
        }
    }
    
    [BurstCompile, WithAll(typeof(HealthComponent))]
    public partial struct FireDamageJob : IJobEntity
    {
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

            ECB.AppendToBuffer(sortKey, entity, new DamageBuffer
            {
                HealthDamage = damage,
                ArmorDamage = damage,
                ShieldDamage = damage,
            });
            
            ECB.AddComponent<PendingDamageTag>(sortKey, entity);

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
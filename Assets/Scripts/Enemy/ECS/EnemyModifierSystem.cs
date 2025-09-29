using Unity.Entities;
using Effects.ECS;
using Unity.Burst;
using Unity.Jobs;

namespace Enemy.ECS
{
    [BurstCompile, UpdateAfter(typeof(SpawnerSystem))]
    public partial struct EnemyModifierSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<EnemySpawnedTag>().Build());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            JobHandle speedHandle = default;
            if (SystemAPI.TryGetSingleton(out EnemySpeedModifierComponent speedModifier))
            {
                speedHandle = new ModifySpeedJob
                {
                    Multiplier = speedModifier.SpeedMultiplier
                }.ScheduleParallel(speedHandle);
            }
            
            JobHandle damageHandle = default;
            if (SystemAPI.TryGetSingleton(out EnemyDamageModifierComponent damageModifier))
            {
                damageHandle = new ModifyDamageJob
                {
                    Multiplier = damageModifier.DamageMultiplier
                }.ScheduleParallel(damageHandle);
            }

            JobHandle modifierHandle = JobHandle.CombineDependencies(speedHandle, damageHandle);
            modifierHandle.Complete();
            
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            new RemoveTagJob
            {
                ECB = ecb.AsParallelWriter(),
            }.ScheduleParallel();
        } 

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
    
    [BurstCompile, WithAll(typeof(EnemySpawnedTag))]
    public partial struct RemoveTagJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        
        public void Execute([ChunkIndexInQuery]int sortKey, Entity entity)
        {
            ECB.RemoveComponent<EnemySpawnedTag>(sortKey, entity);
        }
    }

    [BurstCompile, WithAll(typeof(EnemySpawnedTag))]
    public partial struct ModifySpeedJob : IJobEntity
    {
        public float Multiplier;
        
        public void Execute(ref SpeedComponent speed)
        {
            speed.Speed *= Multiplier;  
        }
    }
    
    
    [BurstCompile, WithAll(typeof(EnemySpawnedTag))]
    public partial struct ModifyDamageJob : IJobEntity
    {
        public float Multiplier;
        
        public void Execute(ref SimpleDamageComponent damage)
        {
            damage.Damage *= Multiplier;  
        }
    }

    public struct EnemySpeedModifierComponent : IComponentData
    {
        public float SpeedMultiplier;
    }
    
    public struct EnemyDamageModifierComponent : IComponentData
    {
        public float DamageMultiplier;
    }
}
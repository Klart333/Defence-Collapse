using Effects.ECS;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Enemy.ECS
{
    [BurstCompile, UpdateAfter(typeof(SpawnerSystem))]
    public partial struct EnemyModifierSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            EntityQueryBuilder builder = new EntityQueryBuilder(state.WorldUpdateAllocator).WithAll<EnemySpawnedTag>();
            state.RequireForUpdate(state.GetEntityQuery(builder));       
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
                damageHandle = new ModifyDamageJob()
                {
                    Multiplier = damageModifier.DamageMultiplier
                }.ScheduleParallel(damageHandle);
            }

            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
            JobHandle removeHandle = JobHandle.CombineDependencies(speedHandle, damageHandle);
            removeHandle = new RemoveTagJob
            {
                ECB = ecb.AsParallelWriter(),
            }.ScheduleParallel(removeHandle);
            
            removeHandle.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
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
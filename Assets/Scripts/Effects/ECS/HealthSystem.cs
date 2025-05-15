using Health;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Effects.ECS
{
    [UpdateInGroup(typeof(SimulationSystemGroup)), UpdateAfter(typeof(CollisionSystem))]
    public partial struct HealthSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            new HealthJob
            {
                ECB = ecb.AsParallelWriter(),
            }.ScheduleParallel();
            
            state.Dependency.Complete(); 
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
    
    [BurstCompile]
    public partial struct HealthJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        
        [BurstCompile]
        public void Execute([ChunkIndexInQuery]int index, Entity entity, ref HealthComponent health, in PendingDamageComponent pendingDamage)
        {
            float damageTaken;
            HealthType damageType;
            if (health.Shield > 0)
            {
                damageType = HealthType.Shield;
                damageTaken = pendingDamage.ShieldDamage;
                health.Shield -= damageTaken;
            }
            else if (health.Armor > 0)
            {
                damageType = HealthType.Armor;
                damageTaken = pendingDamage.ArmorDamage;
                health.Armor -= damageTaken;
            }
            else
            {
                damageType = HealthType.Health;
                damageTaken = pendingDamage.HealthDamage;
                health.Health -= pendingDamage.HealthDamage;
            }
            
            ECB.RemoveComponent<PendingDamageComponent>(index, entity);
            ECB.AddComponent(index, entity, new DamageTakenComponent
            {
                DamageTaken = damageTaken,
                DamageTakenType = damageType,
                IsCrit = pendingDamage.IsCrit,
            });
            
            if (health.Health <= 0)
            {
                ECB.AddComponent(index, entity, new DeathTag());
            }
        }
    }
}
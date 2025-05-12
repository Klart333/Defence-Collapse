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
        public void Execute([ChunkIndexInQuery]int index, Entity entity, ref HealthComponent health)
        {
            if (health.PendingDamage <= 0) // Probably change to a new pendingdamage component
            {
                return;
            }
            
            float damageTaken = health.PendingDamage;
            HealthType damageType;
            if (health.Shield > 0)
            {
                damageType = HealthType.Shield;
                health.Shield -= damageTaken;
            }
            else if (health.Armor > 0)
            {
                damageType = HealthType.Armor;
                health.Armor -= damageTaken;
            }
            else
            {
                damageType = HealthType.Health;
                health.Health -= damageTaken; // Process resistance
            }
            health.PendingDamage = 0;
            
            ECB.AddComponent(index, entity, new DamageTakenComponent
            {
                DamageTaken = damageTaken,
                DamageTakenType = damageType
            });
            
            if (health.Health <= 0)
            {
                ECB.AddComponent(index, entity, new DeathTag());
            }
        }
    }
}
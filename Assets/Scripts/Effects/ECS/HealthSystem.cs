using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

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
        public void Execute([ChunkIndexInQuery]int index, Entity entity, ref HealthComponent component)
        {
            if (component.PendingDamage <= 0)
            {
                return;
            }
            
            float damageTaken = component.PendingDamage;
            component.Health -= damageTaken; // Process resistance
            component.PendingDamage = 0;
            
            ECB.AddComponent(index, entity, new DamageTakenComponent { DamageTaken = damageTaken });
            
            if (component.Health <= 0)
            {
                ECB.AddComponent(index, entity, new DeathTag());
            }
        }
    }
}
using Health;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Effects.ECS
{
    [UpdateInGroup(typeof(SimulationSystemGroup)), UpdateAfter(typeof(CollisionSystem))]
    public partial struct HealthSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<DamageCallbackSingletonTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            Entity bufferEntity = SystemAPI.GetSingletonEntity<DamageCallbackSingletonTag>();
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

            state.Dependency = new HealthJob
            {
                ECB = ecb.AsParallelWriter(),
                BufferEntity = bufferEntity,
            }.ScheduleParallel(state.Dependency);
            
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
        public Entity BufferEntity;
        
        public EntityCommandBuffer.ParallelWriter ECB;
        
        [BurstCompile]
        public void Execute([ChunkIndexInQuery]int index, Entity entity, ref HealthComponent health, in PendingDamageComponent pendingDamage, in LocalTransform transform)
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

            ECB.AppendToBuffer(index, BufferEntity, new DamageCallbackComponent
            {
                DamageTaken = damageTaken,
                DamageTakenType = damageType,
                Position = transform.Position,
                
                Key = pendingDamage.Key,
                TriggerDamageDone = pendingDamage.TriggerDamageDone
            });
            
            if (health.Health <= 0)
            {
                ECB.AddComponent(index, entity, new DeathTag());
            }
        }
    }

    [InternalBufferCapacity(0)]
    public struct DamageCallbackComponent : IBufferElementData
    {
        public int Key;
        public float DamageTaken;
        public HealthType DamageTakenType;
        public float3 Position;

        public bool TriggerDamageDone;
    }
    
    public struct DamageCallbackSingletonTag : IComponentData { }
}
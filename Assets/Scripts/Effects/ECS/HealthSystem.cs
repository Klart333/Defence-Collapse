using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Effects.ECS.ECB;
using Unity.Entities;
using Unity.Burst;
using Enemy.ECS;
using Health;

namespace Effects.ECS
{
    [BurstCompile, UpdateAfter(typeof(BeforeHealthECBSystem))]
    public partial struct HealthSystem : ISystem
    {
        private BufferLookup<DamageBuffer> damageBufferLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            damageBufferLookup = SystemAPI.GetBufferLookup<DamageBuffer>();

            state.RequireForUpdate<DamageCallbackSingletonTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            Entity bufferEntity = SystemAPI.GetSingletonEntity<DamageCallbackSingletonTag>();
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
            
            damageBufferLookup.Update(ref state);

            state.Dependency = new HealthJob
            {
                DamageBufferLookup = damageBufferLookup,
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
    
    [BurstCompile, WithAll(typeof(PendingDamageTag))]
    public partial struct HealthJob : IJobEntity
    {
        [ReadOnly]
        public BufferLookup<DamageBuffer> DamageBufferLookup;
        
        public Entity BufferEntity;
        
        public EntityCommandBuffer.ParallelWriter ECB;
        
        [BurstCompile]
        public void Execute([ChunkIndexInQuery]int index, Entity entity, ref HealthComponent health, in LocalTransform transform)
        {
            DynamicBuffer<DamageBuffer> damageBuffer = DamageBufferLookup[entity];
            for (int i = 0; i < damageBuffer.Length; ++i)
            {
                TakeDamage(ref health, damageBuffer[i], out float damageTaken, out HealthType damageType);
                
                ECB.AppendToBuffer(index, entity, new DamageTakenBuffer
                {
                    DamageTaken = damageTaken,
                    DamageTakenType = damageType,
                    IsCrit = damageBuffer[i].IsCrit,
                });

                ECB.AppendToBuffer(index, BufferEntity, new DamageCallbackComponent
                {
                    DamageTaken = damageTaken,
                    DamageTakenType = damageType,
                    Position = transform.Position,
                
                    Key = damageBuffer[i].Key,
                    TriggerDamageDone = damageBuffer[i].TriggerDamageDone
                });

            }
            
            ECB.AddComponent<DamageTakenTag>(index, entity);
            
            ECB.RemoveComponent<PendingDamageTag>(index, entity);
            ECB.SetBuffer<DamageBuffer>(index, entity);
            
            if (health.Health <= 0)
            {
                ECB.AddComponent<DeathTag>(index, entity);
            }
        }

        private static void TakeDamage(ref HealthComponent health, DamageBuffer damageBuffer, out float damageTaken, out HealthType damageType)
        {
            if (health.Shield > 0)
            {
                damageType = HealthType.Shield;
                damageTaken = damageBuffer.ShieldDamage;
                health.Shield -= damageTaken;
            }
            else if (health.Armor > 0)
            {
                damageType = HealthType.Armor;
                damageTaken = damageBuffer.ArmorDamage;
                health.Armor -= damageTaken;
            }
            else
            {
                damageType = HealthType.Health;
                damageTaken = damageBuffer.HealthDamage;
                health.Health -= damageBuffer.HealthDamage;
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
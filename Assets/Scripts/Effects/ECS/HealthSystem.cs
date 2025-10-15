using System.Runtime.CompilerServices;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Effects.ECS.ECB;
using Unity.Entities;
using Unity.Burst;
using Enemy.ECS;

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
        public void Execute([ChunkIndexInQuery]int index, Entity entity, ref HealthComponent health, in ArmorComponent armorComponent, in LocalTransform transform)
        {
            DynamicBuffer<DamageBuffer> damageBuffer = DamageBufferLookup[entity];
            for (int i = 0; i < damageBuffer.Length; ++i)
            {
                float damageTaken = TakeDamage(ref health, armorComponent, damageBuffer[i]);
                
                ECB.AppendToBuffer(index, entity, new DamageTakenBuffer
                {
                    DamageTaken = damageTaken,
                    IsCrit = damageBuffer[i].IsCrit,
                });

                ECB.AppendToBuffer(index, BufferEntity, new DamageCallbackComponent
                {
                    DamageTaken = damageTaken,
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float TakeDamage(ref HealthComponent health, ArmorComponent armorComponent, DamageBuffer damageBuffer)
        {
            float penetratedDamage = damageBuffer.Damage * damageBuffer.ArmorPenetration;
            float damage = damageBuffer.Damage - penetratedDamage;

            bool hasArmor = health.Armor > 0;
            if (hasArmor && armorComponent.Armor > 0)
            {
                damage *= 100f / (100f + armorComponent.Armor);
            }
            else if (armorComponent.Armor < 0)
            {
                float multiplier = (100f - armorComponent.Armor) / 100f;
                
                damage *= multiplier;
                penetratedDamage *= multiplier;
            }
            
            float damageDone = damage + penetratedDamage;
            if (hasArmor)
            {
                float absorbed = math.min(health.Armor, damage);
                health.Armor -= absorbed;
                damage -= absorbed;
            }
            
            health.Health -= damage + penetratedDamage; // Left over damge after damaging armor
            
            return damageDone;
            
        }
    }

    [InternalBufferCapacity(0)]
    public struct DamageCallbackComponent : IBufferElementData
    {
        public int Key;
        public float DamageTaken;
        public float3 Position;

        public bool TriggerDamageDone;
    }
    
    public struct DamageCallbackSingletonTag : IComponentData { }
}
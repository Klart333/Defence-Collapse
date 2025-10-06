using Unity.Collections;
using Effects.ECS.ECB;
using Enemy.ECS;
using Unity.Entities;
using Unity.Burst;

namespace Effects.ECS
{
    [BurstCompile, UpdateAfter(typeof(BeforeDamageEffectsECBSystem)), UpdateBefore(typeof(BeforeHealthECBSystem))]
    public partial struct FireCollisionSystem : ISystem
    {
        private ComponentLookup<FireComponent> fireComponentLookup;
        private BufferLookup<DamageBuffer> damageBuffer;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            fireComponentLookup = SystemAPI.GetComponentLookup<FireComponent>(true);
            damageBuffer = SystemAPI.GetBufferLookup<DamageBuffer>();
                
            state.RequireForUpdate<BeforeHealthECBSystem.Singleton>();
            state.RequireForUpdate<PendingDamageTag>();
            state.RequireForUpdate<FireComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            fireComponentLookup.Update(ref state);
            damageBuffer.Update(ref state);
            
            var singleton = SystemAPI.GetSingleton<BeforeHealthECBSystem.Singleton>();
            EntityCommandBuffer ecb = singleton.CreateCommandBuffer(state.WorldUnmanaged);

            state.Dependency = new FireCollisionJob
            {
                DamageBufferLookup = damageBuffer,
                FireLookup = fireComponentLookup,
                ECB = ecb.AsParallelWriter(),
            }.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }

    [BurstCompile, WithAll(typeof(PendingDamageTag))]
    public partial struct FireCollisionJob : IJobEntity
    {
        [ReadOnly]
        public ComponentLookup<FireComponent> FireLookup;

        [ReadOnly]
        public BufferLookup<DamageBuffer> DamageBufferLookup;
        
        public EntityCommandBuffer.ParallelWriter ECB;
        
        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity)
        {
            DynamicBuffer<DamageBuffer> damageBuffer = DamageBufferLookup[entity];

            float totalFirePower = 0;
            for (int i = 0; i < damageBuffer.Length; i++)
            {
                if (entity.Equals(damageBuffer[i].SourceEntity) ||
                    !FireLookup.TryGetComponent(damageBuffer[i].SourceEntity, out FireComponent sourceFire)) continue;
                totalFirePower += sourceFire.TotalDamage;
            }

            if (totalFirePower <= 0)
            {
                return;
            }
            
            if (!FireLookup.TryGetComponent(entity, out FireComponent fire))
            {
                fire = new FireComponent();
            }
            
            fire.TotalDamage += totalFirePower;
            ECB.AddComponent(sortKey, entity, fire);
        }
    }
}
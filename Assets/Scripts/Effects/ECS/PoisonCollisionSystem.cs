using Unity.Collections;
using Effects.ECS.ECB;
using Enemy.ECS;
using Unity.Entities;
using Unity.Burst;

namespace Effects.ECS
{
    [BurstCompile, UpdateAfter(typeof(BeforeDamageEffectsECBSystem)), UpdateBefore(typeof(BeforeHealthECBSystem))]
    public partial struct PoisonCollisionSystem : ISystem
    {
        private ComponentLookup<PoisonComponent> poisonComponentLookup;
        private BufferLookup<DamageBuffer> damageBufferLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            poisonComponentLookup = SystemAPI.GetComponentLookup<PoisonComponent>(true);
            damageBufferLookup = SystemAPI.GetBufferLookup<DamageBuffer>();
            
            state.RequireForUpdate<BeforeHealthECBSystem.Singleton>();
            state.RequireForUpdate<PendingDamageTag>();
            state.RequireForUpdate<PoisonComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var singleton = SystemAPI.GetSingleton<BeforeHealthECBSystem.Singleton>();
            EntityCommandBuffer ecb = singleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            poisonComponentLookup.Update(ref state);
            damageBufferLookup.Update(ref state);

            new PoisonCollisionJob
            {
                DamageBufferLookup = damageBufferLookup,
                PoisonLookup = poisonComponentLookup,
                ECB = ecb.AsParallelWriter(),
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
    
    [BurstCompile, WithAll(typeof(PendingDamageTag))]
    public partial struct PoisonCollisionJob : IJobEntity
    {
        [ReadOnly]
        public ComponentLookup<PoisonComponent> PoisonLookup;

        [ReadOnly]
        public BufferLookup<DamageBuffer> DamageBufferLookup;
        
        public EntityCommandBuffer.ParallelWriter ECB;

        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity)
        {
            DynamicBuffer<DamageBuffer> damageBuffer = DamageBufferLookup[entity];

            float totalPoisonPower = 0;
            for (int i = 0; i < damageBuffer.Length; i++)
            {
                if (entity.Equals(damageBuffer[i].SourceEntity) ||
                    !PoisonLookup.TryGetComponent(damageBuffer[i].SourceEntity, out PoisonComponent sourcePoison)) continue;
                totalPoisonPower += sourcePoison.TotalDamage;
            }

            if (totalPoisonPower <= 0)
            {
                return;
            }
            
            if (!PoisonLookup.TryGetComponent(entity, out PoisonComponent poison))
            {
                poison = new PoisonComponent();                
            }
            
            poison.TotalDamage += totalPoisonPower;
            ECB.AddComponent(sortKey, entity, poison);
        }
    }
}
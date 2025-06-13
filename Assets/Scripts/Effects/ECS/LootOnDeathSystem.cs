using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Effects.ECS
{
    [UpdateAfter(typeof(HealthSystem))]
    public partial class LootOnDeathSystem : SystemBase
    {
        private NativeQueue<float3> lootPositionsQueue;
    
        protected override void OnStartRunning()
        {
            EntityQueryBuilder builder = new EntityQueryBuilder(WorldUpdateAllocator).WithAll<DeathTag, LootOnDeathComponent>();
            RequireForUpdate(GetEntityQuery(builder));
            
            lootPositionsQueue = new NativeQueue<float3>(Allocator.Persistent);
        }

        protected override void OnUpdate()
        {
            Dependency = new LootOnDeathJob
            {
                lootPositionsQueue = lootPositionsQueue.AsParallelWriter(),
            }.ScheduleParallel(Dependency);
            
            Dependency.Complete();

            while (lootPositionsQueue.TryDequeue(out float3 lootPos))
            {
                ECSEvents.OnLootSpawn?.Invoke(lootPos);
            }
        }

        protected override void OnStopRunning()
        {
            lootPositionsQueue.Dispose();
        }
    }
    
    
    [BurstCompile, WithAll(typeof(DeathTag))]
    public partial struct LootOnDeathJob : IJobEntity
    {
        public NativeQueue<float3>.ParallelWriter lootPositionsQueue;
        
        public void Execute(in LocalTransform transform, in LootOnDeathComponent loot, ref RandomComponent random)
        {
            float nextFloat = random.Random.NextFloat();
            if (nextFloat > loot.Probability)
            {
                return;
            }
            
            lootPositionsQueue.Enqueue(transform.Position);
        }
    }

    public struct LootOnDeathComponent : IComponentData
    {
        public float Probability;
    }
}
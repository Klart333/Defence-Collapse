using System.Collections.Generic;
using Pathfinding;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Enemy.ECS
{
    [UpdateAfter(typeof(CheckAttackingSystem))]
    public partial class StopAttackingSystem : SystemBase   
    {
        public static readonly Queue<PathIndex> KilledIndexes = new Queue<PathIndex>();
        
        protected override void OnUpdate()
        {
            int count = KilledIndexes.Count;
            if (count <= 0)
            {
                return;
            }
            
            NativeArray<PathIndex> indexes = new NativeArray<PathIndex>(count, Allocator.TempJob);
            for (int i = 0; i < count; i++)
            {
                indexes[i] = KilledIndexes.Dequeue();
            }
                
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(World.Unmanaged);
            new StopAttackingJob
            {
                ECB = ecb.AsParallelWriter(),
                KilledIndexes = indexes,
                Length = count,
            }.ScheduleParallel();
        }
    }
    
    [BurstCompile]
    public partial struct StopAttackingJob : IJobEntity
    {
        [ReadOnly, DeallocateOnJobCompletion]
        public NativeArray<PathIndex> KilledIndexes;

        public EntityCommandBuffer.ParallelWriter ECB;
        
        public int Length;
        
        [BurstCompile]
        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in AttackingComponent attackingComponent)
        {
            for (int i = 0; i < Length; i++)
            {
                if (!attackingComponent.Target.Equals(KilledIndexes[i])) continue;
                
                ECB.RemoveComponent<AttackingComponent>(sortKey, entity);
                ECB.RemoveComponent<UpdateClusterAttackingComponent>(sortKey, entity);
            }
        }
    }
}
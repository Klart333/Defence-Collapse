using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Pathfinding;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace DataStructures.Queue.ECS
{
    [UpdateAfter(typeof(CheckAttackingSystem))]
    public partial class StopAttackingSystem : SystemBase   
    {
        public static readonly Queue<int> KilledIndexes = new Queue<int>();
        
        protected override void OnUpdate()
        {
            int count = KilledIndexes.Count;
            if (count <= 0)
            {
                return;
            }
            
            NativeArray<int> indexes = new NativeArray<int>(count, Allocator.TempJob);
            for (int i = 0; i < count; i++)
            {
                indexes[i] = KilledIndexes.Dequeue();
            }
                
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            new StopAttackingJob
            {
                ECB = ecb.AsParallelWriter(),
                KilledIndexes = indexes,
                Length = count,
            }.ScheduleParallel();
                
            Dependency.Complete();
            ecb.Playback(EntityManager);
            ecb.Dispose();
            indexes.Dispose();
        }
    }
    
    [BurstCompile]
    public partial struct StopAttackingJob : IJobEntity
    {
        [ReadOnly]
        public NativeArray<int> KilledIndexes;

        public EntityCommandBuffer.ParallelWriter ECB;
        
        public int Length;
        
        [BurstCompile]
        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in AttackingComponent attackingComponent)
        {
            for (int i = 0; i < Length; i++)
            {
                if (attackingComponent.Target == KilledIndexes[i])
                {
                    ECB.RemoveComponent<AttackingComponent>(sortKey, entity);
                }
            }
        }
    }
}
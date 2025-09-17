using System.Collections.Generic;
using Gameplay.Turns.ECS;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Burst;
using Effects.ECS;
using Pathfinding;
using System;
using UnityEngine;

namespace Enemy.ECS
{
    [UpdateAfter(typeof(CheckAttackingSystem))]
    public partial class AttackingSystem : SystemBase 
    {
        public static readonly Dictionary<PathIndex, Action<float>> DamageEvent = new Dictionary<PathIndex, Action<float>>();
        
        private BufferLookup<ManagedEntityBuffer> bufferLookup;
        private NativeQueue<DamageIndex> damageQueue;

        private EntityQuery updateEnemiesQuery;

        protected override void OnCreate()
        {
            damageQueue = new NativeQueue<DamageIndex>(Allocator.Persistent);
            bufferLookup = SystemAPI.GetBufferLookup<ManagedEntityBuffer>();
            
            RequireForUpdate<UpdateClusterAttackingComponent>();
        }

        protected override void OnUpdate()
        {
            bufferLookup.Update(this);
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
            
            Dependency = new AttackingJob
            {
                DamageQueue = damageQueue.AsParallelWriter(),
                BufferLookup = bufferLookup,
                ECB = ecb.AsParallelWriter(),
            }.ScheduleParallel(Dependency);
            
            Dependency.Complete();
            ecb.Playback(EntityManager);
            ecb.Dispose();

            HashSet<PathIndex> failedAttacks = new HashSet<PathIndex>();
            while (damageQueue.TryDequeue(out DamageIndex item))
            {
                if (DamageEvent.TryGetValue(item.Index, out Action<float> action))
                {
                    action.Invoke(item.Damage);
                }
                else
                {
                    failedAttacks.Add(item.Index);
                }
            }

            foreach (PathIndex failedAttack in failedAttacks)
            {
                StopAttackingSystem.KilledIndexes.Enqueue(failedAttack);
            }
        }
        
        protected override void OnDestroy()
        {
            damageQueue.Dispose();
        }
    }
    
    [BurstCompile, WithNone(typeof(MovingClusterComponent))]
    public partial struct AttackingJob : IJobEntity
    {
        [ReadOnly]
        public BufferLookup<ManagedEntityBuffer> BufferLookup;
        
        public NativeQueue<DamageIndex>.ParallelWriter DamageQueue;
        public EntityCommandBuffer.ParallelWriter ECB;
        
        [BurstCompile]
        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in AttackingComponent attackingComponent, ref UpdateClusterAttackingComponent updateClusterComponent, in SimpleDamageComponent damageComponent, in SpeedComponent speed)
        {
            updateClusterComponent.Count--;
            if (updateClusterComponent.Count <= 0)
            {
                ECB.RemoveComponent<UpdateClusterAttackingComponent>(sortKey, entity);
            }
            
            ECB.AddComponent(sortKey, entity, new MovingClusterComponent { TimeLeft = 0.2f * speed.Speed });
            
            int enemyCount = BufferLookup[entity].Length;
            float damage = damageComponent.Damage * enemyCount;
            DamageQueue.Enqueue(new DamageIndex { Damage = damage, Index = attackingComponent.Target });
        }
    }
    
    public struct DamageIndex
    {
        public float Damage;
        public PathIndex Index;
    }
}
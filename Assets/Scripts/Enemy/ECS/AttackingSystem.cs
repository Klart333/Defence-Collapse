using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using UnityEngine;
using System;
using Pathfinding;

namespace DataStructures.Queue.ECS
{
    public partial struct AttackingSystem : ISystem
    {
        public static readonly Dictionary<PathIndex, Action<float>> DamageEvent = new Dictionary<PathIndex, Action<float>>();
        
        private NativeQueue<DamageIndex> damageQueue;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            damageQueue = new NativeQueue<DamageIndex>(Allocator.Persistent);
        }
        
        public void OnUpdate(ref SystemState state)
        {
            new AttackingJob
            {
                DamageQueue = damageQueue.AsParallelWriter(),
                DeltaTime = SystemAPI.Time.DeltaTime,
            }.ScheduleParallel();

            state.Dependency.Complete();

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

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            damageQueue.Dispose();
        }
    }
    
    [BurstCompile]
    public partial struct AttackingJob : IJobEntity
    {
        public NativeQueue<DamageIndex>.ParallelWriter DamageQueue;
        
        public float DeltaTime;
        
        [BurstCompile]
        public void Execute(AttackingAspect attackingAspect)
        {
            if (!attackingAspect.CanAttack(DeltaTime))
            {
                return;    
            }
            attackingAspect.AttackSpeedComponent.ValueRW.Timer = 0;

            float damage = attackingAspect.DamageComponent.ValueRO.Damage;
            DamageQueue.Enqueue(new DamageIndex { Damage = damage, Index = attackingAspect.AttackingComponent.ValueRO.Target });
        }
    }
    
    public struct DamageIndex
    {
        public float Damage;
        public PathIndex Index;
    }
}
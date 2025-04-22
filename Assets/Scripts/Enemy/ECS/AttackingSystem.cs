using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using UnityEngine;
using System;
using Gameplay;
using Pathfinding;

namespace DataStructures.Queue.ECS
{
    public partial class AttackingSystem : SystemBase 
    {
        public static readonly Dictionary<PathIndex, Action<float>> DamageEvent = new Dictionary<PathIndex, Action<float>>();
        
        private NativeQueue<DamageIndex> damageQueue;

        protected override void OnCreate()
        {
            damageQueue = new NativeQueue<DamageIndex>(Allocator.Persistent);
        }

        protected override void OnUpdate()
        {
            float gameSpeed = SystemAPI.GetSingleton<GameSpeedComponent>().Speed;

            new AttackingJob
            {
                DamageQueue = damageQueue.AsParallelWriter(),
                DeltaTime = SystemAPI.Time.DeltaTime * gameSpeed,
            }.ScheduleParallel();

            Dependency.Complete();

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
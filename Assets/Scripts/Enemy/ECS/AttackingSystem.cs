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
        
        private NativeQueue<DamageIndex> damageQueue;
        private BufferLookup<ManagedEntityBuffer> bufferLookup;

        protected override void OnCreate()
        {
            damageQueue = new NativeQueue<DamageIndex>(Allocator.Persistent);
            bufferLookup = SystemAPI.GetBufferLookup<ManagedEntityBuffer>();
            
            RequireForUpdate<TurnIncreaseComponent>();
        }

        protected override void OnUpdate()
        {
            TurnIncreaseComponent turnIncrease = SystemAPI.GetSingleton<TurnIncreaseComponent>();
            bufferLookup.Update(this);
            
            Dependency = new AttackingJob
            {
                DamageQueue = damageQueue.AsParallelWriter(),
                TurnIncrease = turnIncrease.TurnIncrease,
                BufferLookup = bufferLookup,
            }.ScheduleParallel(Dependency);
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
    
    [BurstCompile, WithAll(typeof(EnemyClusterComponent))]
    public partial struct AttackingJob : IJobEntity
    {
        [ReadOnly]
        public BufferLookup<ManagedEntityBuffer> BufferLookup;
        
        public NativeQueue<DamageIndex>.ParallelWriter DamageQueue;
        
        public int TurnIncrease;
        
        [BurstCompile]
        public void Execute(Entity entity, in AttackingComponent attackingComponent, ref AttackSpeedComponent attackSpeedComponent, in SimpleDamageComponent damageComponent)
        {
            attackSpeedComponent.AttackTimer -= TurnIncrease;
            if (attackSpeedComponent.AttackTimer > 0) return;
            attackSpeedComponent.AttackTimer += attackSpeedComponent.AttackSpeed;

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
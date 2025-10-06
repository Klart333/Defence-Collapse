using Unity.Collections;
using Enemy.ECS.Boss;
using Unity.Entities;
using Unity.Burst;
using System;

namespace Enemy.ECS
{
    [BurstCompile, UpdateAfter(typeof(SpawnerSystem)), UpdateBefore(typeof(EnemyModifierSystem))]  
    public partial struct EnemyAddComponentSystem : ISystem
    {
        private NativeArray<EnemyAddComponents> AllAddComponents;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<EnemyAddComponent>();
            state.RequireForUpdate<EnemySpawnedTag>();
            
            AllAddComponents = new NativeArray<EnemyAddComponents>(1, Allocator.Persistent);
            AllAddComponents[0] = EnemyAddComponents.WinOnDeath;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            new EnemyAddComponentJob
            {
                AllAddComponents = AllAddComponents,
                ECB = ecb.AsParallelWriter(),
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            AllAddComponents.Dispose();
        }
    }

    [BurstCompile, WithAll(typeof(EnemySpawnedTag))]
    public partial struct EnemyAddComponentJob : IJobEntity
    {
        [ReadOnly]
        public NativeArray<EnemyAddComponents> AllAddComponents;

        public EntityCommandBuffer.ParallelWriter ECB;
        
        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in EnemyAddComponent addComponent, in ManagedClusterComponent managedCluster)
        {
            ECB.RemoveComponent<EnemyAddComponent>(sortKey, entity);
            for (int i = 0; i < AllAddComponents.Length; i++)
            {
                EnemyAddComponents component = AllAddComponents[i];
                if ((addComponent.AddComponents & component) > 0)
                {
                    switch (component)
                    {
                        case EnemyAddComponents.WinOnDeath:
                            ECB.AddComponent<WinOnDeathTag>(sortKey, addComponent.OnCluster ? managedCluster.ClusterParent : entity);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }
    }
}
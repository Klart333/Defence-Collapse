using Unity.Collections;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Burst;
using Effects.ECS;
using Pathfinding;
using System;
using Effects.ECS.ECB;

namespace Enemy.ECS
{
    [BurstCompile, UpdateAfter(typeof(SpawnerSystem)), 
     UpdateAfter(typeof(DeathSystem)), UpdateAfter(typeof(EnemyCountSystem)),
    UpdateBefore(typeof(BeforeCollisionECBSystem))]
    public partial struct EnemyHashGridSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            Entity entity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(entity, new SpatialHashMapSingleton());
            
            state.RequireForUpdate<WaveStateComponent>();
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            int enemyCount = SystemAPI.GetSingleton<WaveStateComponent>().ClusterCount;
            RefRW<SpatialHashMapSingleton> mapSingleton = SystemAPI.GetSingletonRW<SpatialHashMapSingleton>();
            mapSingleton.ValueRW.Value = new NativeParallelHashMap<int2, Entity>((int)(enemyCount * 2.5f) + 100, state.WorldUpdateAllocator); // Double for loadfactor stuff

            if (enemyCount == 0) return;

            state.Dependency = new BuildEnemyHashGridJob
            {
                SpatialGrid = mapSingleton.ValueRW.Value.AsParallelWriter(),
            }.ScheduleParallel(state.Dependency);
            
            state.Dependency.Complete();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            
        }
    }
    
    [BurstCompile, WithAll(typeof(EnemyClusterComponent))]
    public partial struct BuildEnemyHashGridJob : IJobEntity
    {
        [WriteOnly]
        public NativeParallelHashMap<int2, Entity>.ParallelWriter SpatialGrid;

        [BurstCompile]
        public void Execute(in Entity entity, in FlowFieldComponent flowFieldComponent)
        {
            SpatialGrid.TryAdd(PathUtility.GetCombinedIndex(flowFieldComponent.PathIndex), entity);
        }
    }

    public struct SpatialHashMapSingleton : IComponentData, IDisposable
    {
        public NativeParallelHashMap<int2, Entity> Value;

        public void Dispose()
        {
            Value.Dispose();
        }
    }
}
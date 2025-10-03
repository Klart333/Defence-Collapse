using Effects.LittleDudes;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Entities;
using Unity.Burst;
using Effects.ECS;
using System;
using Pathfinding;

namespace Enemy.ECS
{
    [BurstCompile, UpdateAfter(typeof(SpawnerSystem)), 
     UpdateAfter(typeof(DeathSystem)), UpdateAfter(typeof(EnemyCountSystem))]
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
            mapSingleton.ValueRW.Value = new NativeParallelHashMap<int, Entity>((int)(enemyCount * 2.5f) + 100, state.WorldUpdateAllocator); // Double for loadfactor stuff
            if (enemyCount == 0)
            {
                return;
            }

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
    
    [BurstCompile]
    public partial struct BuildEnemyHashGridJob : IJobEntity
    {
        [WriteOnly]
        public NativeParallelHashMap<int, Entity>.ParallelWriter SpatialGrid;

        [BurstCompile]
        public void Execute(in Entity entity, in EnemyClusterComponent enemyClusterComponent)
        {
            int gridIndex = PathUtility.GetCombinedIndex(enemyClusterComponent.Position.xz);
            SpatialGrid.TryAdd(gridIndex, entity);
        }
    }

    public struct SpatialHashMapSingleton : IComponentData, IDisposable
    {
        public NativeParallelHashMap<int, Entity> Value;

        public void Dispose()
        {
            Value.Dispose();
        }
    }
}
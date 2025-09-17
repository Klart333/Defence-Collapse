using Effects.LittleDudes;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Entities;
using Unity.Burst;
using Effects.ECS;
using System;

namespace Enemy.ECS
{
    [BurstCompile, UpdateAfter(typeof(SpawnerSystem)), UpdateAfter(typeof(DeathSystem)), UpdateAfter(typeof(EnemyCountSystem))]
    public partial struct EnemyHashGridSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var entity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(entity, new SpatialHashMapSingleton());
            
            state.RequireForUpdate<WaveStateComponent>();
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            int enemyCount = SystemAPI.GetSingleton<WaveStateComponent>().EnemyCount;
            RefRW<SpatialHashMapSingleton> mapSingleton = SystemAPI.GetSingletonRW<SpatialHashMapSingleton>();
            mapSingleton.ValueRW.Value = new NativeParallelMultiHashMap<int2, Entity>((int)(enemyCount * 2.5f) + 100, state.WorldUpdateAllocator); // Double for loadfactor stuff
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
    [WithAll(typeof(ManagedClusterComponent))]
    public partial struct BuildEnemyHashGridJob : IJobEntity // CELL SIZE 1!!
    {
        [WriteOnly]
        public NativeParallelMultiHashMap<int2, Entity>.ParallelWriter SpatialGrid;

        [BurstCompile]
        public void Execute(in LocalTransform enemyTransform, in Entity entity)
        {
            int2 cell = HashGridUtility.GetCellForCellSize1(enemyTransform.Position.xz);
            SpatialGrid.Add(cell, entity);
        }
    }

    public struct SpatialHashMapSingleton : IComponentData, IDisposable
    {
        public NativeParallelMultiHashMap<int2, Entity> Value;

        public void Dispose()
        {
            Value.Dispose();
        }
    }
    
    public static class HashGridUtility
    {
        public static int2 GetCell(float2 position, float cellSize)
        {
            int cellX = (int)(position.x / cellSize);
            int cellY = (int)(position.y / cellSize);
            return new int2(cellX, cellY);
        }
        
        public static int2 GetCell(float3 position, float cellSize)
        {
            int cellX = (int)(position.x / cellSize);
            int cellY = (int)(position.z / cellSize);
            return new int2(cellX, cellY);
        }
        
        public static int2 GetCellForCellSize1(float2 position)
        {
            int cellX = (int)position.x;
            int cellY = (int)position.y;
            return new int2(cellX, cellY);
        }
    }
}
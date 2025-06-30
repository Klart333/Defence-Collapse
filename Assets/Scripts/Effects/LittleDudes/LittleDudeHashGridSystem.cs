using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.Entities;
using Unity.Burst;
using Effects.ECS;
using Enemy.ECS;
using System;

namespace Effects.LittleDudes
{
    [BurstCompile, UpdateInGroup(typeof(SimulationSystemGroup)), UpdateAfter(typeof(DeathSystem))]
    public partial struct LittleDudeHashGridSystem : ISystem
    {
        private EntityQuery littleDudeQuery;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            Entity entity = state.EntityManager.CreateEntity(); 
            state.EntityManager.AddComponentData(entity, new LittleDudeSpatialHashMapSingleton());

            littleDudeQuery = SystemAPI.QueryBuilder().WithAll<LittleDudeComponent>().Build();
            state.RequireForUpdate<LittleDudeComponent>();
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            int count = littleDudeQuery.CalculateEntityCount();
            
            RefRW<LittleDudeSpatialHashMapSingleton> mapSingleton = SystemAPI.GetSingletonRW<LittleDudeSpatialHashMapSingleton>();
            mapSingleton.ValueRW.Value = new NativeParallelMultiHashMap<int2, Entity>(count * 2 + 10, state.WorldUpdateAllocator); // Double for loadfactor stuff

            state.Dependency = new BuildLittleDudeHashGridJob
            {
                SpatialGrid = mapSingleton.ValueRW.Value.AsParallelWriter(),
                CellSize = 1,
            }.ScheduleParallel(state.Dependency);
            
            //state.Dependency.Complete();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            
        }
    }
    
    [BurstCompile]
    [WithAll(typeof(FlowFieldComponent), typeof(LittleDudeComponent))]
    public partial struct BuildLittleDudeHashGridJob : IJobEntity
    {
        [WriteOnly]
        public NativeParallelMultiHashMap<int2, Entity>.ParallelWriter SpatialGrid;

        public float CellSize;

        [BurstCompile]
        public void Execute(Entity entity, in LocalTransform enemyTransform)
        {
            int2 cell = HashGridUtility.GetCell(enemyTransform.Position, CellSize);
            SpatialGrid.Add(cell, entity);
        }
    }

    public struct LittleDudeSpatialHashMapSingleton : IComponentData, IDisposable
    {
        public NativeParallelMultiHashMap<int2, Entity> Value;

        public void Dispose()
        {
            Value.Dispose();
        }
    }
}
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Entities;
using Unity.Burst;

namespace DataStructures.Queue.ECS
{
    [BurstCompile]
    public partial class EnemyHashGridSystem : SystemBase
    {
        public NativeParallelMultiHashMap<int2, Entity> SpatialGrid;

        [BurstCompile]
        protected override void OnCreate()
        {
            base.OnCreate();
            
            SpatialGrid = new NativeParallelMultiHashMap<int2, Entity>(20000, Allocator.Persistent);
        }

        [BurstCompile]
        protected override void OnUpdate()
        {
            SpatialGrid.Clear();
            new BuildEnemyHashGridJob
            {
                SpatialGrid = SpatialGrid.AsParallelWriter(),
                CellSize = 1,
            }.ScheduleParallel();
        }

        [BurstCompile]
        protected override void OnDestroy()
        {
            SpatialGrid.Dispose();
        }
    }
    
    [BurstCompile]
    [WithAll(typeof(FlowFieldComponent))]
    public partial struct BuildEnemyHashGridJob : IJobEntity
    {
        public NativeParallelMultiHashMap<int2, Entity>.ParallelWriter SpatialGrid;

        public float CellSize;

        [BurstCompile]
        public void Execute(in LocalTransform enemyTransform, in Entity entity)
        {
            int2 cell = HashGridUtility.GetCell(enemyTransform.Position, CellSize);
            SpatialGrid.Add(cell, entity);
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
    }
}
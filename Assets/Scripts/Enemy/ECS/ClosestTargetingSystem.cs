using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DataStructures.Queue.ECS
{
    public partial struct ClosestTargetingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnemyHashGridSystem>();
        }

        public void OnUpdate(ref SystemState state)
        {
            // Get the spatial grid from the BuildSpatialGridSystem
            NativeParallelMultiHashMap<int2, Entity> spatialGrid = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<EnemyHashGridSystem>().SpatialGrid;

            // Create and schedule the job
            new ClosestTargetingJob
            {
                transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
                SpatialGrid = spatialGrid,
                CellSize = 1
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}
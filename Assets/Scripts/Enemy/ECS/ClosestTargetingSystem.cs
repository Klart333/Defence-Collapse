using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace DataStructures.Queue.ECS
{
    [UpdateAfter(typeof(EnemyHashGridSystem))]
    public partial struct ClosestTargetingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        { 
        }

        public void OnUpdate(ref SystemState state)
        {
            // Get the spatial grid from the BuildSpatialGridSystem
            NativeParallelMultiHashMap<int2, Entity> spatialGrid = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<EnemyHashGridSystem>().SpatialGrid;

            // Create and schedule the job
            new ClosestTargetingJob
            {
                transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
                SpatialGrid = spatialGrid.AsReadOnly(),
                CellSize = 1
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}
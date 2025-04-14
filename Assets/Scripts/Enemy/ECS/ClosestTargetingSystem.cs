using Gameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
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
                TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
                SpatialGrid = spatialGrid.AsReadOnly(),
                CellSize = 1,
                DeltaTime = SystemAPI.Time.DeltaTime * GameSpeedManager.Instance.GameSpeed,
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
    
    [BurstCompile]
    public partial struct ClosestTargetingJob : IJobEntity
    {
        [ReadOnly]
        public ComponentLookup<LocalTransform> TransformLookup;
        
        [ReadOnly, NativeDisableContainerSafetyRestriction]
        public NativeParallelMultiHashMap<int2, Entity>.ReadOnly SpatialGrid;

        public float DeltaTime;
        public float CellSize;

        [BurstCompile]
        public void Execute(EnemyTargetAspect enemyTargetAspect)
        {
            if (!enemyTargetAspect.ShouldFindTarget(DeltaTime))
            {
                return;
            }
            
            float2 towerPosition = enemyTargetAspect.LocalTransform.ValueRO.Position.xz;
            int2 towerCell = HashGridUtility.GetCell(towerPosition, CellSize);

            // Calculate the number of cells to check based on the tower's range
            int radiusCells = Mathf.CeilToInt(enemyTargetAspect.RangeComponent.ValueRO.Range / CellSize);

            float bestDistSq = enemyTargetAspect.RangeComponent.ValueRO.Range * enemyTargetAspect.RangeComponent.ValueRO.Range;
            Entity bestEnemy = Entity.Null;

            // Iterate over nearby cells
            for (int dx = -radiusCells; dx <= radiusCells; dx++)
            for (int dy = -radiusCells; dy <= radiusCells; dy++)
            {
                int2 cell = new int2(towerCell.x + dx, towerCell.y + dy);

                // Check if the cell exists in the grid
                if (!SpatialGrid.TryGetFirstValue(cell, out Entity enemy, out var iterator)) continue;
                
                do
                {
                    RefRO<LocalTransform> enemyTransform = TransformLookup.GetRefRO(enemy);
                    
                    float2 enemyPosition = new float2(enemyTransform.ValueRO.Position.x, enemyTransform.ValueRO.Position.z);
                    float distSq = math.distancesq(towerPosition, enemyPosition);

                    // Check if this enemy is closer
                    if (distSq < bestDistSq)
                    {
                        bestDistSq = distSq;
                        bestEnemy = enemy;
                    }
                    // COULD EARLY EXIT IF A "GOOD ENOUGH" TARGET IS FOUND

                } while (SpatialGrid.TryGetNextValue(out enemy, ref iterator));
            }

            // Store the closest enemy for this tower
            enemyTargetAspect.EnemyTargetComponent.ValueRW.Target = bestEnemy;
        }
    }
    
    public struct RangeComponent : IComponentData
    {
        public float Range;
    }

    public struct EnemyTargetComponent : IComponentData
    {
        public Entity Target;
    }
}
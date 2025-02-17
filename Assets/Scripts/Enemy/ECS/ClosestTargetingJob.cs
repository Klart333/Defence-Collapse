using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Entities;
using UnityEngine;

namespace DataStructures.Queue.ECS
{
    [BurstCompile]
    public partial struct ClosestTargetingJob : IJobEntity
    {
        [ReadOnly]
        public ComponentLookup<LocalTransform> transformLookup;
        
        [ReadOnly]
        public NativeParallelMultiHashMap<int2, Entity> SpatialGrid;

        public float CellSize;

        [BurstCompile]
        public void Execute(in LocalTransform towerTransform, in RangeComponent rangeComponent, ref EnemyTargetComponent targetComponent)
        {
            float2 towerPosition = new float2(towerTransform.Position.x, towerTransform.Position.z);

            // Get the grid cell of the tower
            int2 towerCell = HashGridUtility.GetCell(towerPosition, CellSize);

            // Calculate the number of cells to check based on the tower's range
            int radiusCells = Mathf.CeilToInt(rangeComponent.Range / CellSize);

            float bestDistSq = rangeComponent.Range * rangeComponent.Range;
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
                    if (!transformLookup.TryGetComponent(enemy, out LocalTransform enemyTransform)) continue;

                    float2 enemyPosition = new float2(enemyTransform.Position.x, enemyTransform.Position.z);
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
            targetComponent.Target = bestEnemy;
        }
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
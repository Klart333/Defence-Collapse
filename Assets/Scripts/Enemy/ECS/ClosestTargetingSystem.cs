using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.Entities;
using Unity.Burst;
using UnityEngine;
using Gameplay;

namespace Enemy.ECS
{
    [UpdateAfter(typeof(EnemyHashGridSystem))]
    public partial struct ClosestTargetingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameSpeedComponent>();
            state.RequireForUpdate<FlowFieldComponent>();
            
            EntityQuery query = new EntityQueryBuilder(state.WorldUpdateAllocator).WithAspect<EnemyTargetAspect>().Build(ref state);
            state.RequireForUpdate(query);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            NativeParallelMultiHashMap<int2, Entity> spatialGrid = SystemAPI.GetSingletonRW<SpatialHashMapSingleton>().ValueRO.Value;

            // Create and schedule the job
            new ClosestTargetingJob
            {
                TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
                SpatialGrid = spatialGrid.AsReadOnly(),
                CellSize = 1,
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
        
        [ReadOnly]
        public NativeParallelMultiHashMap<int2, Entity>.ReadOnly SpatialGrid;

        public float CellSize;

        [BurstCompile]
        public void Execute(EnemyTargetAspect enemyTargetAspect)
        {
            if (!enemyTargetAspect.ShouldFindTarget())
            {
                return;
            }
            
            float2 towerPosition = enemyTargetAspect.LocalTransform.ValueRO.Position.xz;
            int2 towerCell = HashGridUtility.GetCell(towerPosition, CellSize);
            float range = enemyTargetAspect.RangeComponent.ValueRO.Range;
            int radiusCells = Mathf.CeilToInt(range / CellSize);
            float rangeSq = range * range;
            float maxCellDist = range + CellSize * 1.414f; // Diagonal of cell
            float maxCellDistSq = maxCellDist * maxCellDist;

            // Common target finding variables
            float bestDistSq = rangeSq;
            float3 bestEnemyPosition = default;

            if (enemyTargetAspect.DirectionComponent.ValueRO.Angle >= 360f)
            {
                // Optimized 360-degree path
                for (int dx = -radiusCells; dx <= radiusCells; dx++)
                for (int dy = -radiusCells; dy <= radiusCells; dy++)
                {
                    int2 cell = new int2(towerCell.x + dx, towerCell.y + dy);
                    
                    // Quick distance check at cell level
                    float2 cellCenter = ((float2)cell + 0.5f) * CellSize;
                    float2 toCell = cellCenter - towerPosition;
                    float cellDistSq = math.lengthsq(toCell);
                    if (cellDistSq > maxCellDistSq
                        || cellDistSq > bestDistSq)
                        continue;

                    if (!SpatialGrid.TryGetFirstValue(cell, out Entity enemy, out var iterator)) 
                        continue;
                    
                    do
                    {
                        RefRO<LocalTransform> enemyTransform = TransformLookup.GetRefRO(enemy);
                        float2 enemyPosition = enemyTransform.ValueRO.Position.xz;
                        float distSq = math.distancesq(towerPosition, enemyPosition);

                        if (distSq < bestDistSq)
                        {
                            bestDistSq = distSq;
                            bestEnemyPosition = enemyTransform.ValueRO.Position;
                        }
                    } while (SpatialGrid.TryGetNextValue(out enemy, ref iterator));
                }
            }
            else
            {
                // Directional cone path
                float2 direction = enemyTargetAspect.DirectionComponent.ValueRO.Direction;
                float halfAngleRad = math.radians(enemyTargetAspect.DirectionComponent.ValueRO.Angle * 0.5f);
                float cosHalfAngle = math.cos(halfAngleRad);
                float2 coneLeft = math.mul(float2x2.Rotate(halfAngleRad), direction);
                float2 coneRight = math.mul(float2x2.Rotate(-halfAngleRad), direction);

                for (int dx = -radiusCells; dx <= radiusCells; dx++)
                for (int dy = -radiusCells; dy <= radiusCells; dy++)
                {
                    int2 cell = new int2(towerCell.x + dx, towerCell.y + dy);
                    float2 cellCenter = ((float2)cell + 0.5f) * CellSize;
                    float2 toCell = cellCenter - towerPosition;
                    float cellDistSq = math.lengthsq(toCell);

                    // Cell-level culling
                    if (cellDistSq > maxCellDistSq
                        || cellDistSq > bestDistSq)
                        continue;

                    if (math.lengthsq(toCell) > 0.001f) // Avoid division by zero
                    {
                        float2 normToCell = math.normalize(toCell);
                        float dotDirection = math.dot(normToCell, direction);
                        
                        if (dotDirection < cosHalfAngle - 0.1f) // With safety margin
                        {
                            float dotLeft = math.dot(normToCell, coneLeft);
                            float dotRight = math.dot(normToCell, coneRight);
                            if (dotLeft < 0 && dotRight < 0)
                                continue;
                        }
                    }

                    if (!SpatialGrid.TryGetFirstValue(cell, out Entity enemy, out var iterator)) 
                        continue;
                    
                    do
                    {
                        RefRO<LocalTransform> enemyTransform = TransformLookup.GetRefRO(enemy);
                        float2 enemyPosition = enemyTransform.ValueRO.Position.xz + direction;
                        float2 toEnemy = enemyPosition - towerPosition;
                        float distSq = math.lengthsq(toEnemy);

                        if (distSq < bestDistSq)
                        {
                            float2 normToEnemy = math.normalize(toEnemy);
                            if (math.dot(normToEnemy, direction) >= cosHalfAngle)
                            {
                                bestDistSq = distSq;
                                bestEnemyPosition = enemyTransform.ValueRO.Position;
                            }
                        }
                    } while (SpatialGrid.TryGetNextValue(out enemy, ref iterator));
                }
            }

            if (!bestEnemyPosition.Equals(default))
            {
                enemyTargetAspect.EnemyTargetComponent.ValueRW.TargetPosition = bestEnemyPosition;
                enemyTargetAspect.EnemyTargetComponent.ValueRW.HasTarget = true;
            }
        }
    }
    
    public struct RangeComponent : IComponentData
    {
        public float Range;
    }
    
    public struct DirectionRangeComponent : IComponentData
    {
        public float2 Direction;
        public float Angle;
    }

    public struct EnemyTargetComponent : IComponentData
    {
        public float3 TargetPosition;
        public bool HasTarget;
    }
}
using Buildings.District.ECS;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.Entities;
using Unity.Burst;

namespace Enemy.ECS
{
    [BurstCompile, UpdateAfter(typeof(EnemyHashGridSystem))]
    public partial struct ClosestTargetingSystem : ISystem
    {
        private ComponentLookup<LocalTransform> transformLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TargetingActivationComponent>();

            transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            NativeParallelMultiHashMap<int2, Entity> spatialGrid = SystemAPI.GetSingletonRW<SpatialHashMapSingleton>().ValueRO.Value;
            transformLookup.Update(ref state);
            
            new ClosestTargetingJob
            {
                TransformLookup = transformLookup,
                SpatialGrid = spatialGrid.AsReadOnly(),
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
    
    // CELL SIZE = 1
    [BurstCompile, WithAll(typeof(TargetingActivationComponent))]
    public partial struct ClosestTargetingJob : IJobEntity 
    {   
        [ReadOnly]
        public ComponentLookup<LocalTransform> TransformLookup;
        
        [ReadOnly]
        public NativeParallelMultiHashMap<int2, Entity>.ReadOnly SpatialGrid;
        
        [BurstCompile]
        public void Execute(in DirectionRangeComponent directionComponent, ref EnemyTargetComponent enemyTargetComponent, in RangeComponent rangeComponent, in LocalTransform localTransform)
        {
            float2 towerPosition = localTransform.Position.xz;
            int2 towerCell = HashGridUtility.GetCellForCellSize1(towerPosition);
            float range = rangeComponent.Range;
            int radiusCells = (int)math.ceil(range);
            float rangeSq = range * range;
            float maxCellDist = range * 1.414f; // Diagonal of cell
            float maxCellDistSq = maxCellDist * maxCellDist;

            // Common target finding variables
            float bestDistSq = rangeSq;
            float3 bestEnemyPosition = default;

            if (directionComponent.Angle >= 360f)
            {
                // Optimized 360-degree path
                for (int dx = -radiusCells; dx <= radiusCells; dx++)
                for (int dy = -radiusCells; dy <= radiusCells; dy++)
                {
                    int2 cell = new int2(towerCell.x + dx, towerCell.y + dy);
                    
                    // Quick distance check at cell level
                    float2 cellCenter = (float2)cell + 0.5f;
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

                        if (distSq >= bestDistSq) continue;
                        
                        bestDistSq = distSq;
                        bestEnemyPosition = enemyTransform.ValueRO.Position;
                    } while (SpatialGrid.TryGetNextValue(out enemy, ref iterator));
                }
            }
            else
            {
                // Directional cone path
                float2 direction = directionComponent.Direction;
                float halfAngleRad = math.radians(directionComponent.Angle * 0.5f);
                float cosHalfAngle = math.cos(halfAngleRad);
                float2 coneLeft = math.mul(float2x2.Rotate(halfAngleRad), direction);
                float2 coneRight = math.mul(float2x2.Rotate(-halfAngleRad), direction);

                for (int dx = -radiusCells; dx <= radiusCells; dx++)
                for (int dy = -radiusCells; dy <= radiusCells; dy++)
                {
                    int2 cell = new int2(towerCell.x + dx, towerCell.y + dy);
                    float2 cellCenter = (float2)cell + 0.5f;
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

                        if (distSq >= bestDistSq) continue;
                        
                        float2 normToEnemy = math.normalize(toEnemy);
                        if (math.dot(normToEnemy, direction) < cosHalfAngle) continue;
                        
                        bestDistSq = distSq;
                        bestEnemyPosition = enemyTransform.ValueRO.Position;
                    } while (SpatialGrid.TryGetNextValue(out enemy, ref iterator));
                }
            }

            if (bestEnemyPosition.Equals(default)) return;
            
            enemyTargetComponent.TargetPosition = bestEnemyPosition;
            enemyTargetComponent.HasTarget = true;
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
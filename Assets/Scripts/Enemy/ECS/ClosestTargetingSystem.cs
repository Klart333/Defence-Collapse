using Buildings.District.ECS;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.Entities;
using Unity.Burst;
using Pathfinding;
using Utility;

namespace Enemy.ECS
{
    [BurstCompile, UpdateAfter(typeof(EnemyHashGridSystem))]
    public partial struct ClosestTargetingSystem : ISystem
    {
        private ComponentLookup<LocalTransform> transformLookup;
        private BufferLookup<ManagedEntityBuffer> bufferLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<UpdateTargetingTag>();

            transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            bufferLookup = SystemAPI.GetBufferLookup<ManagedEntityBuffer>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            NativeParallelHashMap<int, Entity> spatialGrid = SystemAPI.GetSingletonRW<SpatialHashMapSingleton>().ValueRO.Value;
            transformLookup.Update(ref state);
            bufferLookup.Update(ref state);

            state.Dependency = new ClosestTargetingJob
            {
                TransformLookup = transformLookup,
                SpatialGrid = spatialGrid.AsReadOnly(),
                BufferLookup = bufferLookup,
            }.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
    
    // CELL SIZE = 1
    [BurstCompile, WithAll(typeof(UpdateTargetingTag))]
    public partial struct ClosestTargetingJob : IJobEntity 
    {   
        [ReadOnly]
        public ComponentLookup<LocalTransform> TransformLookup;
        
        [ReadOnly]
        public NativeParallelHashMap<int, Entity>.ReadOnly SpatialGrid;
        
        [ReadOnly]
        public BufferLookup<ManagedEntityBuffer> BufferLookup;
        
        [BurstCompile]
        public void Execute(in DirectionRangeComponent directionComponent, ref EnemyTargetComponent enemyTargetComponent, 
            in RangeComponent rangeComponent, in LocalTransform localTransform)
        {
            
            float2 towerPosition = localTransform.Position.xz;
            int towerCell = PathUtility.GetPathGridIndex(towerPosition);
            
            float range = rangeComponent.Range;
            float rangeSq = range * range;
            float maxCellDist = range * 1.414f; // Diagonal of cell
            float maxCellDistSq = maxCellDist * maxCellDist;
            int radiusCells = (int)(range / PathUtility.CELL_SCALE + 0.5f);

            int capacity = (radiusCells + 1) * (radiusCells + 1) * 4;
            using MyNativePriorityHeap<int> priorityHeap = new MyNativePriorityHeap<int>(capacity, Allocator.Temp);
            // Common target finding variables 
            float bestDistSq = rangeSq;
            float3 bestEnemyPosition = default;

            if (directionComponent.Angle >= 360f)
            {
                // Optimized 360-degree path
                for (int dx = -radiusCells; dx <= radiusCells; dx++)
                for (int dy = -radiusCells; dy <= radiusCells; dy++)
                {
                    int cell = towerCell + dx + dy * PathUtility.CELL_SCALE;
                    
                    // Quick distance check at cell level
                    float2 cellCenter = PathUtility.GetPos(cell) + PathUtility.HALF_CELL_SCALE;
                    float2 toCell = cellCenter - towerPosition;
                    float cellDistSq = math.lengthsq(toCell);
                    
                    if (cellDistSq > maxCellDistSq || cellDistSq > bestDistSq) continue;

                    bestDistSq = GetClosestEnemy(cell, towerPosition, bestDistSq, ref bestEnemyPosition);
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
                    int cell = towerCell + dx + dy * PathUtility.CELL_SCALE;
                    float2 cellCenter = PathUtility.GetPos(cell) + PathUtility.HALF_CELL_SCALE;
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

                    bestDistSq = GetClosestEnemy(cell, towerPosition, bestDistSq, ref bestEnemyPosition);
                }
            }
            
            if (bestEnemyPosition.Equals(default)) return;
            
            enemyTargetComponent.TargetPosition = bestEnemyPosition;
            enemyTargetComponent.HasTarget = true;
        }

        private float GetClosestEnemy(int cell, float2 towerPosition, float bestDistSq, ref float3 bestEnemyPosition)
        {
            if (!SpatialGrid.TryGetValue(cell, out Entity cluster) || !BufferLookup.TryGetBuffer(cluster, out DynamicBuffer<ManagedEntityBuffer> buffer)) 
                return bestDistSq;

            for (int i = 0; i < buffer.Length; i++)
            {
                Entity enemy = buffer[i].Entity;
                LocalTransform enemyTransform = TransformLookup[enemy];
                float2 enemyPosition = enemyTransform.Position.xz;
                float distSq = math.distancesq(towerPosition, enemyPosition);

                if (distSq >= bestDistSq) continue;
                        
                bestDistSq = distSq;
                bestEnemyPosition = enemyTransform.Position;
            } 

            return bestDistSq;
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
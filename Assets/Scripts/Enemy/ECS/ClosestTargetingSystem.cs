using Buildings.District.ECS;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.Entities;
using Unity.Burst;
using Pathfinding;
using Utility;
using System;

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
    
    [BurstCompile(FloatPrecision.Low, FloatMode.Fast), WithAll(typeof(UpdateTargetingTag))]
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
            PathIndex towerCell = PathUtility.GetIndex(towerPosition);
            
            float range = rangeComponent.Range;
            float rangeSq = range * range;
            float maxCellDist = range * 1.414f; // Diagonal of cell
            float maxCellDistSq = maxCellDist * maxCellDist;
            int radiusCells = (int)(range / PathUtility.CELL_SCALE + 0.5f);

            MyNativePriorityHeap<DistanceIndex> frontier = new MyNativePriorityHeap<DistanceIndex>(radiusCells * radiusCells * 4, Allocator.Temp);
            NativeHashSet<PathIndex> reached = new NativeHashSet<PathIndex>((radiusCells + 1) * (radiusCells + 1) * 4, Allocator.Temp);
            NativeArray<PathIndex> neighbours = new NativeArray<PathIndex>(4, Allocator.Temp);
            
            frontier.Push(new DistanceIndex { Distance = 0, PathIndex = towerCell, });
            
            float3 bestEnemyPosition = default;
            float2 direction = directionComponent.Direction;
            float halfAngleRad = math.radians(directionComponent.Angle * 0.5f);
            float cosHalfAngle = math.cos(halfAngleRad);
            float2 coneLeft = math.mul(float2x2.Rotate(halfAngleRad), direction);
            float2 coneRight = math.mul(float2x2.Rotate(-halfAngleRad), direction);

            while (frontier.Count > 0)
            {
                PathIndex index = frontier.Pop().PathIndex;
                float2 position = PathUtility.GetPos(index).xz;
                float2 cellCenter = PathUtility.GetPos(index).xz + PathUtility.HALF_CELL_SCALE;
                float2 toCell = cellCenter - towerPosition;
                
                if (!IsIndexWithinRange(toCell, maxCellDistSq))
                    continue;
                
                if (directionComponent.Angle < 360f && !IsIndexWithinAngle(toCell, direction, coneLeft, coneRight, cosHalfAngle))
                    continue;

                if (CellContainsEnemy(index, towerPosition, rangeSq, out bestEnemyPosition))
                    break;
                
                PathUtility.NativeGetNeighbouringPathIndexes(neighbours, position);
                for (int i = 0; i < neighbours.Length; i++)
                {
                    if (!reached.Add(neighbours[i])) continue;
                    
                    int distance = PathUtility.GetDistance(towerCell, neighbours[i]);
                    frontier.Push(new DistanceIndex { Distance = distance, PathIndex = neighbours[i] });
                }
            }
            
            frontier.Dispose();
            reached.Dispose();
            neighbours.Dispose();
            
            if (bestEnemyPosition.Equals(default)) return;
            
            enemyTargetComponent.TargetPosition = bestEnemyPosition;
            enemyTargetComponent.HasTarget = true;
        }

        private bool CellContainsEnemy(PathIndex index, float2 towerPosition, float bestDistSq, out float3 bestEnemyPosition)
        {
            bestEnemyPosition = default;
            int cell = PathUtility.GetCombinedIndex(index);
            if (!SpatialGrid.TryGetValue(cell, out Entity cluster) || !BufferLookup.TryGetBuffer(cluster, out DynamicBuffer<ManagedEntityBuffer> buffer)) 
                return false;

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

            return true;
        }

        private bool IsIndexWithinAngle(float2 toCell, float2 direction, float2 coneLeft, float2 coneRight, float cosHalfAngle)
        {
            if (math.lengthsq(toCell) < 0.001f) return true;
            
            float2 normToCell = math.normalize(toCell);
            float dotDirection = math.dot(normToCell, direction);

            if (dotDirection > cosHalfAngle - 0.1f) return true; 
            
            float dotLeft = math.dot(normToCell, coneLeft);
            float dotRight = math.dot(normToCell, coneRight);
            return dotLeft >= 0 || dotRight >= 0;
        }
        
        private bool IsIndexWithinRange(float2 toCell, float maxDistanceSq)
        {
            float cellDistSq = math.lengthsq(toCell);
            return cellDistSq < maxDistanceSq;
        }

        public struct DistanceIndex : IComparable<DistanceIndex>
        {
            public int Distance;
            public PathIndex PathIndex;

            public int CompareTo(DistanceIndex other)
            {
                return Distance.CompareTo(other.Distance);
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
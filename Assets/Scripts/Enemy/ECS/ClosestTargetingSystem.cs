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
            NativeParallelHashMap<int2, Entity> spatialGrid = SystemAPI.GetSingletonRW<SpatialHashMapSingleton>().ValueRO.Value;
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
        public NativeParallelHashMap<int2, Entity>.ReadOnly SpatialGrid;
        
        [ReadOnly]
        public BufferLookup<ManagedEntityBuffer> BufferLookup;
        
        [BurstCompile]
        public void Execute(in DirectionRangeComponent directionComponent, ref EnemyTargetComponent enemyTargetComponent, 
            in RangeComponent rangeComponent, in LocalTransform localTransform)
        {
            float2 towerPosition = localTransform.Position.xz;
            int2 towerCell = PathUtility.GetCombinedIndex(towerPosition);
            
            float range = rangeComponent.Range;
            float rangeSq = range * range;
            float maxCellDist = range * 1.414f; // Diagonal of cell
            float maxCellDistSq = maxCellDist * maxCellDist;
            int radiusCells = (int)(range / PathUtility.CELL_SCALE + 0.5f);

            MyNativePriorityHeap<DistanceIndex> frontier = new MyNativePriorityHeap<DistanceIndex>(math.max(radiusCells * radiusCells * 4, 13), Allocator.Temp);
            NativeHashSet<int2> reached = new NativeHashSet<int2>((radiusCells + 1) * (radiusCells + 1) * 4, Allocator.Temp);
            NativeArray<int2> neighbours = new NativeArray<int2>(4, Allocator.Temp);
            
            frontier.Push(new DistanceIndex { Distance = 0, PathIndex = towerCell, });
            
            float3 bestEnemyPosition = default;
            float2 direction = directionComponent.Direction;
            float cosHalfAngle = math.cos(directionComponent.Angle * 0.5f * math.TORADIANS);

            while (frontier.Count > 0)
            {
                int2 index = frontier.Pop().PathIndex;
                float2 cellCenter = PathUtility.GetPos(index) + PathUtility.HALF_CELL_SCALE;
                float2 toCell = cellCenter - towerPosition;
                
                if (!IsIndexWithinRange(toCell, maxCellDistSq))
                    continue;
                
                if (directionComponent.Angle < 360f && !index.Equals(towerCell) && !IsIndexWithinAngle(toCell, direction, cosHalfAngle))
                    continue;

                if (CellContainsEnemy(index, towerPosition, rangeSq, out bestEnemyPosition))
                    break;
                
                PathUtility.NativeGetNeighbouringPathIndexes(neighbours, index);
                for (int i = 0; i < neighbours.Length; i++)
                {
                    if (!reached.Add(neighbours[i])) continue;
                    
                    int distance = PathUtility.GetDistance(towerCell, neighbours[i]);
                    frontier.Push(new DistanceIndex { Distance = distance, PathIndex = neighbours[i] });
                }
            }
            
            neighbours.Dispose();
            frontier.Dispose();
            reached.Dispose();
            
            if (bestEnemyPosition.Equals(default)) return;
            
            enemyTargetComponent.TargetPosition = bestEnemyPosition;
            enemyTargetComponent.HasTarget = true;
        }

        private bool CellContainsEnemy(int2 index, float2 towerPosition, float bestDistSq, out float3 bestEnemyPosition)
        {
            bestEnemyPosition = default;
            if (!SpatialGrid.TryGetValue(index, out Entity cluster) || !BufferLookup.TryGetBuffer(cluster, out DynamicBuffer<ManagedEntityBuffer> buffer)) 
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

        private bool IsIndexWithinAngle(float2 toCell, float2 direction, float cosHalfAngle)
        {
            float2 normToCell = math.normalize(toCell);
            float dotDirection = math.dot(normToCell, direction);

            if (dotDirection < 0) // Can't have angle over 180
            {
                return false;
            }
            
            return dotDirection > cosHalfAngle;
        }
        
        private bool IsIndexWithinRange(float2 toCell, float maxDistanceSq)
        {
            float cellDistSq = math.lengthsq(toCell);
            return cellDistSq < maxDistanceSq;
        }

        public struct DistanceIndex : IComparable<DistanceIndex>
        {
            public int Distance;
            public int2 PathIndex;

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
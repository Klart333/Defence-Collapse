using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.Entities;
using Unity.Burst;
using Enemy.ECS;
using Gameplay;

namespace Effects.LittleDudes
{
    [UpdateAfter(typeof(LittleDudeHashGridSystem))]
    public partial struct PushSystem : ISystem
    {
        private ComponentLookup<LocalTransform> transformLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state) 
        {
            state.RequireForUpdate<GameSpeedComponent>();
            transformLookup = state.GetComponentLookup<LocalTransform>(true);
        }

        public void OnUpdate(ref SystemState state)
        {
            NativeParallelMultiHashMap<int2, Entity> spatialGrid = SystemAPI.GetSingletonRW<LittleDudeSpatialHashMapSingleton>().ValueRO.Value;
            transformLookup.Update(ref state);
            float gameSpeed = SystemAPI.GetSingleton<GameSpeedComponent>().Speed;
            
            new LittleDudePushJob 
            {
                SpatialGrid = spatialGrid.AsReadOnly(),
                DeltaTime = SystemAPI.Time.DeltaTime * gameSpeed,
                TransformLookup = transformLookup,
                CellSize = 1, 
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }

    [BurstCompile, WithAll(typeof(LittleDudeComponent))]
    public partial struct LittleDudePushJob : IJobEntity
    {
        [ReadOnly, NativeDisableContainerSafetyRestriction]
        public ComponentLookup<LocalTransform> TransformLookup;
        
        [ReadOnly]
        public NativeParallelMultiHashMap<int2, Entity>.ReadOnly SpatialGrid;
        
        [ReadOnly]
        public float CellSize;

        [ReadOnly]
        public float DeltaTime;
        
        private const float PUSH_STRENGTH = 3;
        
        [BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
        public void Execute(Entity entity, ref FlowFieldComponent flowField, in LocalTransform transform)
        {
            float2 pusherPosition = new float2(transform.Position.x, transform.Position.z);
            int2 cell = HashGridUtility.GetCell(pusherPosition, CellSize);
            if (!SpatialGrid.TryGetFirstValue(cell, out Entity enemy, out var iterator)) return;
            do
            {
                if (entity.Equals(enemy)) continue;
                
                RefRO<LocalTransform> enemyTransform = TransformLookup.GetRefRO(enemy);
                float2 enemyPosition = new float2(enemyTransform.ValueRO.Position.x, enemyTransform.ValueRO.Position.z);
                float distSq = math.distancesq(pusherPosition, enemyPosition);
                
                if (distSq is > 0.04f or 0) continue; // 0.2 * 0.2
                
                float3 desiredForward = math.normalize(transform.Position - enemyTransform.ValueRO.Position);
                desiredForward.y = 0;
                float3 currentForward = flowField.Forward;

                if (math.abs(math.dot(currentForward, desiredForward)) > 0.999f) return;
                    
                float3 rotationAxis = math.normalize(math.cross(currentForward, desiredForward));
                float angle = math.acos(math.clamp(math.dot(currentForward, desiredForward), -1f, 1f));
                angle = math.min(angle, PUSH_STRENGTH * DeltaTime);
                quaternion rotation = quaternion.AxisAngle(rotationAxis, angle);

                // Apply the rotation to the current forward direction
                flowField.Forward = math.rotate(rotation, currentForward);

                return;

            } while (SpatialGrid.TryGetNextValue(out enemy, ref iterator));
        }
    }
}
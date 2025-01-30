using Unity.Mathematics;
using Unity.Transforms;
using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace DataStructures.Queue.ECS
{
    public partial struct FlowMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            
        }

        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            
            new FlowMovementJob()
            {
                DeltaTime = deltaTime,
                CellScale = PathManager.Instance.CellScale,
                GridWidth = PathManager.Instance.GridWidth,
                Directions = PathManager.Instance.Directions.AsReadOnly(),
            }.Schedule();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }

    [WithAll(typeof(FlowFieldTag))]
    [BurstCompile]
    internal partial struct FlowMovementJob : IJobEntity
    {
        [Unity.Collections.ReadOnly]
        public float DeltaTime;

        [Unity.Collections.ReadOnly]
        public float CellScale;

        [Unity.Collections.ReadOnly]
        public int GridWidth;
        
        [Unity.Collections.ReadOnly, NativeDisableContainerSafetyRestriction]
        public NativeArray<byte>.ReadOnly Directions;

        private void Execute(in SpeedComponent speed, ref LocalTransform transform)
        {
            int index = PathManager.GetIndex(transform.Position.x, transform.Position.z, CellScale, GridWidth);
            float3 direction = PathManager.ByteToDirectionFloat3(Directions[index]);
            transform.Position += direction * speed.Speed * DeltaTime;
        }
    }
}
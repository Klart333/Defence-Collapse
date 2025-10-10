using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Effects.ECS
{
    [BurstCompile, UpdateAfter(typeof(DeathSystem))]
    public partial struct ShakeSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ShakeComponent>();
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float time = (float)SystemAPI.Time.ElapsedTime;

            new ShakeJob
            {
                Time = time
            }.ScheduleParallel();
        }
        
        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
        
        [BurstCompile]
        public partial struct ShakeJob : IJobEntity
        {
            public float Time;

            void Execute(ref LocalTransform transform, ref ShakeComponent shake)
            {
                float3 offset = new float3(
                    (noise.snoise(new float2(Time * shake.Frequency, 1.0f)) - 0.5f) * 2f,
                    (noise.snoise(new float2(Time * shake.Frequency, 2.0f)) - 0.5f) * 2f,
                    (noise.snoise(new float2(Time * shake.Frequency, 3.0f)) - 0.5f) * 2f
                ) * shake.Amplitude;

                transform.Position = shake.OriginalPosition + offset;
            }
        }
        
        public struct ShakeComponent : IComponentData
        {
            public float3 OriginalPosition;
            public float Amplitude;
            public float Frequency;
        }
    }
}
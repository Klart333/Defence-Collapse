using Pathfinding;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DataStructures.Queue.ECS
{
    public partial struct CheckAttackingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            
            new CheckAttackingJob
            {
                ECB = ecb.AsParallelWriter(),
                CellScale = PathManager.Instance.CellScale,
                GridWidth = PathManager.Instance.GridWidth,
                Directions = PathManager.Instance.Directions.AsReadOnly(),
            }.ScheduleParallel();
            
            state.Dependency.Complete(); 
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }

    [BurstCompile, WithNone(typeof(AttackingComponent))]
    public partial struct CheckAttackingJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;

        [ReadOnly]
        public float CellScale;

        [ReadOnly]
        public int GridWidth;
        
        [ReadOnly, NativeDisableContainerSafetyRestriction]
        public NativeArray<byte>.ReadOnly Directions;
        
        [BurstCompile]
        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in FlowFieldComponent flowField, in LocalTransform transform)
        {
            int index = PathManager.GetIndex(transform.Position.x, transform.Position.z, CellScale, GridWidth);

            if (Directions[index] == byte.MaxValue)
            {
                ECB.AddComponent(sortKey, entity, new AttackingComponent { Target = index });
            }
        }
    }

    public struct AttackingComponent : IComponentData
    {
        public int Target;
    }
}
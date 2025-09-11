using Unity.Collections;
using Unity.Mathematics;
using Unity.Entities;
using InputCamera;
using Unity.Burst;

namespace Enemy.ECS
{
    public partial struct ClusterHighlightingSystem : ISystem
    {
        private BufferLookup<ManagedEntityBuffer> bufferLookup;
        private NativeReference<float3> lastHightlightPosition;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MousePositionComponent>();
            state.RequireForUpdate<EnemyClusterComponent>();

            bufferLookup = SystemAPI.GetBufferLookup<ManagedEntityBuffer>();
            lastHightlightPosition = new NativeReference<float3>(Allocator.Persistent);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            MousePositionComponent mousePosition = SystemAPI.GetSingleton<MousePositionComponent>();
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
            bufferLookup.Update(ref state);
            
            state.Dependency = new ClusterHighlightingJob
            { 
                MouseWorldPosition = mousePosition.WorldPosition,
                LastPosition = lastHightlightPosition,
                ECB = ecb.AsParallelWriter(),
                BufferLookup = bufferLookup,
            }.ScheduleParallel(state.Dependency);
            
            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            lastHightlightPosition.Dispose();
        }
    }

    [BurstCompile]
    public partial struct ClusterHighlightingJob : IJobEntity // TODO: Maybe Hash it you know
    {
        [ReadOnly]
        public BufferLookup<ManagedEntityBuffer> BufferLookup;

        [NativeDisableParallelForRestriction]
        public NativeReference<float3> LastPosition;
        
        public EntityCommandBuffer.ParallelWriter ECB;
        
        public float3 MouseWorldPosition;

        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in EnemyClusterComponent cluster)
        {
            if (cluster.Position.Equals(LastPosition.Value))
            {
                return;
            }
            
            float distX = math.abs(MouseWorldPosition.x - cluster.Position.x);
            float distZ = math.abs(MouseWorldPosition.z - cluster.Position.z);
            if (distX >= 1 || distZ >= 1) return;
                
            LastPosition.Value = cluster.Position;
            Entity highlightData = ECB.CreateEntity(sortKey);
            ECB.AddComponent(sortKey, highlightData, new HighlightClusterDataComponent
            {
                EnemyAmount = BufferLookup[entity].Length,
                TargetDirection = cluster.Facing,
                EnemyType = cluster.EnemyType,
                Position = cluster.Position,
            });
        }
    }

    public struct HighlightClusterDataComponent : IComponentData
    {
        public float2 TargetDirection;
        public float3 Position;
        public int EnemyAmount;
        public int EnemyType;
    }
}
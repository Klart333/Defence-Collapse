using Unity.Collections;
using Unity.Entities;
using Effects.ECS;
using Pathfinding;
using Unity.Burst;

namespace Gameplay.Chunk.ECS
{
    [BurstCompile, UpdateAfter(typeof(DeathSystem))]  
    public partial struct RemoveGroundObjectSystem : ISystem 
    { 
        private EntityQuery removeGroundObjectQuery;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        { 
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            removeGroundObjectQuery = SystemAPI.QueryBuilder().WithAll<RemoveGroundObjectComponent>().Build();
            
            state.RequireForUpdate<RemoveGroundObjectComponent>();
            state.RequireForUpdate<GroundObjectComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            NativeArray<RemoveGroundObjectComponent> removes = removeGroundObjectQuery.ToComponentDataArray<RemoveGroundObjectComponent>(Allocator.TempJob);

            new RemoveGroundObjectJob
            {
                ECB = ecb.AsParallelWriter(),
                RemoveGroundObjects = removes
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }

    [BurstCompile]
    public partial struct RemoveGroundObjectJob : IJobEntity
    {
        [ReadOnly, DeallocateOnJobCompletion]
        public NativeArray<RemoveGroundObjectComponent> RemoveGroundObjects;

        public EntityCommandBuffer.ParallelWriter ECB;
        
        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in GroundObjectComponent groundObjectComponent, ref RandomComponent randomComponent)
        {
            for (int i = 0; i < RemoveGroundObjects.Length; i++)
            {
                if (!groundObjectComponent.PathIndex.Equals(RemoveGroundObjects[i].PathIndex)) continue;

                float value = randomComponent.Random.NextFloat();
                if (value <= RemoveGroundObjects[i].Percentage)
                {
                    ECB.AddComponent<DeathTag>(sortKey, entity);
                }
            }
        }
    }
    
    public struct RemoveGroundObjectComponent : IComponentData
    {
        public PathIndex PathIndex;
        public float Percentage;
    }
}
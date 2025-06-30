using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Entities;
using Unity.Burst;

namespace Effects.LittleDudes
{
    public partial struct LittleDudeSpawner : ISystem
    {
        private ComponentLookup<LocalTransform> transformLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<LittleDudePrefabComponent>();
            state.RequireForUpdate<LittleDudeSpawnerDataComponent>();

            transformLookup = SystemAPI.GetComponentLookup<LocalTransform>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
            Entity prefab = SystemAPI.GetSingleton<LittleDudePrefabComponent>().Prefab;
            transformLookup.Update(ref state);

            state.Dependency = new SpawnDudeJob
            {
                ECB = ecb.AsParallelWriter(),
                DudePrefab = prefab,
                TransformLookup = transformLookup,
            }.ScheduleParallel(state.Dependency);
            
            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }

    [BurstCompile]
    public partial struct SpawnDudeJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;

        public Entity DudePrefab;

        [ReadOnly]
        public ComponentLookup<LocalTransform> TransformLookup;
        
        [BurstCompile]
        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in LittleDudeSpawnerDataComponent data)
        {
            for (int i = 0; i < data.Amount; i++)
            {
                Entity dude = ECB.Instantiate(sortKey, DudePrefab);
                
                float3 pos = data.Position + new float3(0.1f * i, 0, 0);
                LocalTransform transform = TransformLookup[DudePrefab];
                transform.Position = pos;
                ECB.SetComponent(sortKey, dude,  transform);
                ECB.SetComponent(sortKey, dude, new LittleDudeComponent
                {
                    HomePosition = data.Position,
                });
             
                ECB.DestroyEntity(sortKey, entity);
            }
        }
    } 

    public struct LittleDudeSpawnerDataComponent : IComponentData
    {
        public float3 Position;
        public int Amount;
    } 
}
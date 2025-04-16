using Unity.Collections;
using Unity.Transforms;
using Unity.Entities;
using Unity.Burst;
using Enemy;
using Gameplay;

namespace DataStructures.Queue.ECS
{
    public partial struct SpawnerSystem : ISystem
    {
        private EntityQuery spawnerQuery;

        public void OnCreate(ref SystemState state)
        {
            spawnerQuery = SystemAPI.QueryBuilder()
                .WithAspect<SpawnPointAspect>()
                .Build();

            state.RequireForUpdate<EnemyDatabaseTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (spawnerQuery.IsEmpty)
            {
                return;
            }
            
            Entity enemyDatabase = SystemAPI.GetSingletonEntity<EnemyDatabaseTag>();
            NativeArray<ItemBufferElement> enemyBuffer = SystemAPI.GetBuffer<ItemBufferElement>(enemyDatabase).AsNativeArray();
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            new SpawnJob
            {
                EnemyBuffer = enemyBuffer,
                DeltaTime = SystemAPI.Time.DeltaTime * GameSpeedManager.Instance.Value,
                ECB = ecb.AsParallelWriter(),
                TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
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
    
    [BurstCompile]
    public partial struct SpawnJob : IJobEntity
    {
        [ReadOnly]
        public NativeArray<ItemBufferElement> EnemyBuffer;

        [ReadOnly]
        public float DeltaTime;
        
        [ReadOnly]
        public ComponentLookup<LocalTransform> TransformLookup;
        
        public EntityCommandBuffer.ParallelWriter ECB; 
        
        [BurstCompile]
        public void Execute([EntityIndexInChunk] int index, Entity entity, SpawnPointAspect spawnPointAspect)
        {
            if (spawnPointAspect.SpawnPointComponent.ValueRO.Timer > 0)
            {
                spawnPointAspect.SpawnPointComponent.ValueRW.Timer -= DeltaTime;
                return;
            }
            
            spawnPointAspect.SpawnPointComponent.ValueRW.Timer = spawnPointAspect.SpawnPointComponent.ValueRO.SpawnRate;
            spawnPointAspect.SpawnPointComponent.ValueRW.Amount--;

            Entity prefabEntity = EnemyBuffer[spawnPointAspect.SpawnPointComponent.ValueRO.EnemyIndex].EnemyEntity;
            Entity spawnedEntity = ECB.Instantiate(index, prefabEntity);
            
            LocalTransform prefabTransform = TransformLookup[prefabEntity];
            LocalTransform newTransform = LocalTransform.FromPositionRotationScale(
                spawnPointAspect.Transform.ValueRO.Position,       
                prefabTransform.Rotation, 
                prefabTransform.Scale     
            );
            ECB.SetComponent(index, spawnedEntity, newTransform);

            if (spawnPointAspect.SpawnPointComponent.ValueRO.Amount <= 0)
            {
                ECB.RemoveComponent<SpawningTag>(index, entity);
            }
        }
    }
}
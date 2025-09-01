using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Entities;
using Effects.ECS;
using Unity.Burst;
using Gameplay;

namespace Enemy.ECS
{
    [UpdateAfter(typeof(DeathSystem))]
    public partial struct SpawnerSystem : ISystem
    {
        private EntityQuery spawnerQuery;
        private ComponentLookup<LocalTransform> transformLookup;

        public void OnCreate(ref SystemState state)
        {
            spawnerQuery = SystemAPI.QueryBuilder().WithAspect<SpawnPointAspect>().Build();

            transformLookup = SystemAPI.GetComponentLookup<LocalTransform>();
            
            state.RequireForUpdate<EnemyBossDatabaseTag>();
            state.RequireForUpdate<GameSpeedComponent>();
            state.RequireForUpdate<EnemyDatabaseTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (spawnerQuery.IsEmpty)
            {
                return;
            }
            
            Entity enemyDatabase = SystemAPI.GetSingletonEntity<EnemyDatabaseTag>();
            Entity bossDatabase = SystemAPI.GetSingletonEntity<EnemyBossDatabaseTag>();
            NativeArray<EnemyBufferElement> enemyBuffer = SystemAPI.GetBuffer<EnemyBufferElement>(enemyDatabase).AsNativeArray();
            NativeArray<EnemyBossElement> bossBuffer = SystemAPI.GetBuffer<EnemyBossElement>(bossDatabase).AsNativeArray();
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            transformLookup.Update(ref state);
            float gameSpeed = SystemAPI.GetSingleton<GameSpeedComponent>().Speed;

            new SpawnJob
            {
                EnemyBuffer = enemyBuffer,
                BossBuffer = bossBuffer,
                DeltaTime = SystemAPI.Time.DeltaTime * gameSpeed,
                ECB = ecb.AsParallelWriter(),
                TransformLookup = transformLookup,
                Seed = UnityEngine.Random.Range(1, 200000000),
                MinRandomPosition = new float3(-0.5f, 0, -0.5f),
                MaxRandomPosition = new float3(0.5f, 0, 0.5f),
            }.ScheduleParallel();

            state.Dependency.Complete(); 
            ecb.Playback(state.EntityManager);
            ecb.Dispose();

            enemyBuffer.Dispose();
            bossBuffer.Dispose();
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
        public NativeArray<EnemyBufferElement> EnemyBuffer;
        
        [ReadOnly]
        public NativeArray<EnemyBossElement> BossBuffer;

        [ReadOnly]
        public float DeltaTime;
        
        [ReadOnly]
        public ComponentLookup<LocalTransform> TransformLookup;
        
        public EntityCommandBuffer.ParallelWriter ECB;

        public float Seed;
        
        public float3 MinRandomPosition;
        public float3 MaxRandomPosition;
        
        [BurstCompile]
        public void Execute([EntityIndexInChunk] int index, Entity entity, SpawnPointAspect spawnPointAspect)
        {
            spawnPointAspect.SpawnPointComponent.ValueRW.Timer -= DeltaTime;
            
            while (spawnPointAspect.SpawnPointComponent.ValueRO.Timer < 0)
            {
                int enemyIndex = spawnPointAspect.SpawnPointComponent.ValueRO.EnemyIndex;
                Entity prefabEntity = enemyIndex >= 100 ? BossBuffer[enemyIndex - 100].EnemyEntity : EnemyBuffer[enemyIndex].EnemyEntity;
                Entity spawnedEntity = ECB.Instantiate(index, prefabEntity);
            
                LocalTransform prefabTransform = TransformLookup[prefabEntity];

                Random random = Random.CreateFromIndex((uint)(Seed + index));
            
                float3 spawnPosition = spawnPointAspect.Transform.ValueRO.Position + random.NextFloat3(MinRandomPosition, MaxRandomPosition);
                quaternion spawnRotation = math.mul(prefabTransform.Rotation, quaternion.AxisAngle(new float3(0, 1, 0), random.NextFloat(360)));

                LocalTransform newTransform = LocalTransform.FromPositionRotationScale(
                    spawnPosition,       
                    spawnRotation, 
                    prefabTransform.Scale     
                );
                ECB.SetComponent(index, spawnedEntity, new RandomComponent { Random = random });
                ECB.SetComponent(index, spawnedEntity, newTransform);
                
                spawnPointAspect.SpawnPointComponent.ValueRW.Timer += spawnPointAspect.SpawnPointComponent.ValueRO.SpawnRate;
                spawnPointAspect.SpawnPointComponent.ValueRW.Amount--;
                
                if (spawnPointAspect.SpawnPointComponent.ValueRO.Amount <= 0)
                {
                    ECB.RemoveComponent<SpawningTag>(index, entity);
                    break;
                }
            } 
        }
    }
}
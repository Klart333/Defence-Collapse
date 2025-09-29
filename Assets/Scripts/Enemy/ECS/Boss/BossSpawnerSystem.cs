using Gameplay.Turns.ECS;
using Unity.Collections;
using Effects.ECS.ECB;
using Unity.Entities;
using Effects.ECS;
using Unity.Burst;
using Unity.Jobs;

namespace Enemy.ECS.Boss
{
    [BurstCompile, UpdateBefore(typeof(SpawnerSystem))]
    public partial struct BossSpawnerSystem : ISystem
    {
        private EntityQuery spawningComponentQuery;
        private EntityQuery spawnPointComponentQuery;
        
        private ComponentLookup<SpawningComponent> spawningComponentLookup;
        private ComponentLookup<SpawnPointComponent> spawnPointComponentLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            spawningComponentQuery = SystemAPI.QueryBuilder().WithAll<SpawningComponent>().Build();
            spawnPointComponentQuery = SystemAPI.QueryBuilder().WithAll<SpawnPointComponent>().Build();
            
            spawningComponentLookup = SystemAPI.GetComponentLookup<SpawningComponent>();
            spawnPointComponentLookup = SystemAPI.GetComponentLookup<SpawnPointComponent>();
            
            state.RequireForUpdate<BeforeSpawningECBSystem.Singleton>();
            state.RequireForUpdate<SpawnBossComponent>();
            state.RequireForUpdate<SpawnPointComponent>();
            state.RequireForUpdate<UpdateEnemiesTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<BeforeSpawningECBSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            SpawnBossComponent spawnBossComponent = SystemAPI.GetSingleton<SpawnBossComponent>();
            Entity spawnBossEntity = SystemAPI.GetSingletonEntity<SpawnBossComponent>();
            
            spawnPointComponentLookup.Update(ref state);
            spawningComponentLookup.Update(ref state);
            
            state.Dependency = new BossSpawnerJob
            {
                SpawnPointEntities = spawnPointComponentQuery.ToEntityArray(Allocator.TempJob),
                SpawningEntities = spawningComponentQuery.ToEntityArray(Allocator.TempJob),
                SpawnPointComponentLookup = spawnPointComponentLookup,
                SpawningComponentLookup = spawningComponentLookup,
                SpawnBossData = spawnBossComponent,
                SpawnBossEntity = spawnBossEntity,
                ECB = ecb,
            }.Schedule();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }

    public struct BossSpawnerJob : IJob
    {
        [ReadOnly, DeallocateOnJobCompletion]
        public NativeArray<Entity> SpawningEntities;
        
        [ReadOnly, DeallocateOnJobCompletion]
        public NativeArray<Entity> SpawnPointEntities;
        
        public ComponentLookup<SpawnPointComponent> SpawnPointComponentLookup;
        public ComponentLookup<SpawningComponent> SpawningComponentLookup;
        
        public SpawnBossComponent SpawnBossData;
        public Entity SpawnBossEntity;

        public EntityCommandBuffer ECB;

        public void Execute()
        {
            RefRW<SpawnPointComponent> spawnPoint = default;
            Entity spawnPointEntity = Entity.Null;

            for (int i = 0; i < SpawnPointEntities.Length; i++)
            {
                ECB.AddComponent<DeathTag>(SpawnPointEntities[i]);
                
                if (spawnPointEntity != Entity.Null) continue;
                
                RefRW<SpawnPointComponent> point = SpawnPointComponentLookup.GetRefRW(SpawnPointEntities[i]);
                if (point.ValueRO.Index != SpawnBossData.SpawnPointIndex) continue;
                
                spawnPointEntity = SpawnPointEntities[i];
                spawnPoint = point;
            }

            if (spawnPoint.ValueRO.IsSpawning)
            {
                for (int i = 0; i < SpawningEntities.Length; i++)
                {
                    RefRW<SpawningComponent> point = SpawningComponentLookup.GetRefRW(SpawningEntities[i]);
                    if (point.ValueRO.SpawnPoint != spawnPointEntity) continue;
                    point.ValueRW = new SpawningComponent
                    {
                        Position = spawnPoint.ValueRO.Position,
                        Random = spawnPoint.ValueRO.Random,
                        EnemyIndex = SpawnBossData.BossIndex,
                        SpawnPoint = spawnPointEntity,
                        Amount = 1,
                        Turns = 5,
                    };
                    
                    ECB.AddComponent(SpawningEntities[i], new EnemyAddComponent
                    {
                        AddComponents = EnemyAddComponents.WinOnDeath,
                        OnCluster = true,
                    });
                    break;
                }
            }
            else
            {
                spawnPoint.ValueRW.IsSpawning = true;
                Entity spawned = ECB.CreateEntity();
                ECB.AddComponent(spawned, new SpawningComponent
                {
                    Position = spawnPoint.ValueRO.Position,
                    Random = spawnPoint.ValueRO.Random,
                    EnemyIndex = SpawnBossData.BossIndex,
                    SpawnPoint = spawnPointEntity,
                    Amount = 1,
                    Turns = 5,
                });
                
                ECB.AddComponent(spawned, new EnemyAddComponent
                {
                    AddComponents = EnemyAddComponents.WinOnDeath,
                    OnCluster = true,
                });
            }
            
            ECB.AddComponent<DeathTag>(SpawnBossEntity);
        }
    }
}
using Gameplay.Turns.ECS;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Entities;
using Effects.ECS;
using Pathfinding;
using Pathfinding.ECS;
using Unity.Burst;

namespace Enemy.ECS
{
    [UpdateAfter(typeof(DeathSystem))]
    public partial struct SpawnerSystem : ISystem
    {
        private ComponentLookup<LocalTransform> transformLookup;
        private ComponentLookup<SpawnPointComponent> spawnPointLookup;
        private EntityQuery spawnerQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PathBlobber>();
            transformLookup = SystemAPI.GetComponentLookup<LocalTransform>();
            spawnPointLookup = SystemAPI.GetComponentLookup<SpawnPointComponent>();

            spawnerQuery = SystemAPI.QueryBuilder().WithAll<SpawnPointComponent>().Build();
            
            state.RequireForUpdate<TurnIncreaseComponent>();
            state.RequireForUpdate<EnemyBossDatabaseTag>();
            state.RequireForUpdate<SpawnPointComponent>();
            state.RequireForUpdate<EnemyDatabaseTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            TurnIncreaseComponent turnIncrease = SystemAPI.GetSingleton<TurnIncreaseComponent>();
            if (turnIncrease.TurnIncrease <= 0) return; 
            
            UpdateSpawners(ref state, turnIncrease.TurnIncrease);
            SpawnSpawners(ref state, turnIncrease);
        }

        [BurstCompile]
        private void UpdateSpawners(ref SystemState state, int turnsIncrease)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

            Entity enemyDatabase = SystemAPI.GetSingletonEntity<EnemyDatabaseTag>();
            Entity bossDatabase = SystemAPI.GetSingletonEntity<EnemyBossDatabaseTag>();
            NativeArray<EnemyBufferElement> enemyBuffer = SystemAPI.GetBuffer<EnemyBufferElement>(enemyDatabase).AsNativeArray();
            NativeArray<EnemyBossElement> bossBuffer = SystemAPI.GetBuffer<EnemyBossElement>(bossDatabase).AsNativeArray();
            PathBlobber pathBlobber = SystemAPI.GetSingleton<PathBlobber>();
            transformLookup.Update(ref state);
            spawnPointLookup.Update(ref state);
            state.Dependency = new SpawnEnemiesJob
            {
                Seed = UnityEngine.Random.Range(1, 200000000), // TODO: Make RandomSeedComponent from GameManager
                ChunkIndexToListIndex = pathBlobber.ChunkIndexToListIndex,
                SpawnPointLookup = spawnPointLookup,
                TransformLookup = transformLookup,
                PathChunks = pathBlobber.PathBlob,
                ECB = ecb.AsParallelWriter(),
                TurnIncrease = turnsIncrease,
                EnemyBuffer = enemyBuffer,
                BossBuffer = bossBuffer,
                StartMoveDelay = 3,
                EnemySpeed = 1,
            }.ScheduleParallel(state.Dependency);

            state.Dependency.Complete(); 
            ecb.Playback(state.EntityManager);

            ecb.Dispose();
            enemyBuffer.Dispose();
            bossBuffer.Dispose();
        }

        private void SpawnSpawners(ref SystemState state, TurnIncreaseComponent turnIncrease)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
            NativeArray<bool> shouldSpawns = GetShouldSpawns(2 * turnIncrease.TurnIncrease);
            NativeArray<SpawningComponent> possibleSpawns = EnemySpawnHandler.SpawnDataUtility.GetSpawnPointData(turnIncrease.TotalTurn);
            state.Dependency = new SpawnSpawningJob
            {
                ECB = ecb.AsParallelWriter(),
                ShouldSpawn = shouldSpawns.AsReadOnly(),
                PossibleSpawns = possibleSpawns.AsReadOnly()
            }.ScheduleParallel(state.Dependency);
            
            state.Dependency.Complete(); 
            ecb.Playback(state.EntityManager);
            
            ecb.Dispose();
            shouldSpawns.Dispose();
            possibleSpawns.Dispose();
        }

        [BurstCompile]
        private NativeArray<bool> GetShouldSpawns(int amountToSpawn)
        {
            int count = spawnerQuery.CalculateEntityCount();
            NativeArray<bool> shouldSpawns = new NativeArray<bool>(count, Allocator.TempJob);
            Random random = Random.CreateFromIndex((uint)UnityEngine.Random.Range(1, 200000000));

            for (int i = 0; i < math.min(count, amountToSpawn); i++)
            {
                shouldSpawns[i] = true;
            }

            for (int i = 0; i < count - 1; i++)
            {
                int index = random.NextInt(i, count);
                (shouldSpawns[i], shouldSpawns[index]) = (shouldSpawns[index], shouldSpawns[i]);
            }
            
            return shouldSpawns;
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
    
    [BurstCompile]
    public partial struct SpawnSpawningJob : IJobEntity
    {
        [ReadOnly]
        public NativeArray<bool>.ReadOnly ShouldSpawn;

        [ReadOnly]
        public NativeArray<SpawningComponent>.ReadOnly PossibleSpawns;
        
        public EntityCommandBuffer.ParallelWriter ECB;
        
        [BurstCompile]
        public void Execute([EntityIndexInChunk] int sortKey, Entity entity, ref SpawnPointComponent spawnPointComponent)
        {
            if (spawnPointComponent.IsSpawning || !ShouldSpawn[spawnPointComponent.Index])
            {
                return;
            }
            spawnPointComponent.IsSpawning = true;
            
            Entity spawned = ECB.CreateEntity(sortKey);
            int index = spawnPointComponent.Random.NextInt(PossibleSpawns.Length);
            ECB.AddComponent(sortKey, spawned, new SpawningComponent
            {
                Position = spawnPointComponent.Position,
                Random = spawnPointComponent.Random,
                EnemyIndex = PossibleSpawns[index].EnemyIndex,
                Amount = PossibleSpawns[index].Amount,
                Turns = PossibleSpawns[index].Turns,
                SpawnPoint = entity
            });
        }
    }

    public partial struct SpawnEnemiesJob : IJobEntity
    {
        [ReadOnly]
        public NativeArray<EnemyBufferElement> EnemyBuffer;
        
        [ReadOnly]
        public NativeArray<EnemyBossElement> BossBuffer;

        [ReadOnly]
        public ComponentLookup<LocalTransform> TransformLookup;
        
        [NativeDisableParallelForRestriction]
        public ComponentLookup<SpawnPointComponent> SpawnPointLookup;
        
        [ReadOnly]
        public BlobAssetReference<PathChunkArray> PathChunks;
        
        [ReadOnly]
        public NativeHashMap<int2, int>.ReadOnly ChunkIndexToListIndex;
        
        public EntityCommandBuffer.ParallelWriter ECB;

        public int StartMoveDelay;
        public float EnemySpeed;
        public int TurnIncrease;
        public int Seed;

        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, ref SpawningComponent spawningComponent)
        {
            spawningComponent.Turns -= TurnIncrease;
            if (spawningComponent.Turns > 0) return;
            
            int enemyIndex = spawningComponent.EnemyIndex;
            Entity prefabEntity = enemyIndex >= 100 ? BossBuffer[enemyIndex - 100].EnemyEntity : EnemyBuffer[enemyIndex].EnemyEntity;
            float3 spawnPosition = spawningComponent.Position;
            
            Entity clusterEntity = ECB.CreateEntity(sortKey);
            DynamicBuffer<ManagedEntityBuffer> buffer = SpawnCluster(sortKey, clusterEntity, spawnPosition);

            for (int i = 0; i < spawningComponent.Amount; i++)
            {
                Entity spawnedEntity = ECB.Instantiate(sortKey, prefabEntity);
            
                LocalTransform prefabTransform = TransformLookup[prefabEntity];
                LocalTransform newTransform = LocalTransform.FromPositionRotationScale(spawnPosition, quaternion.identity, prefabTransform.Scale);
                ECB.SetComponent(sortKey, spawnedEntity, new RandomComponent { Random = Random.CreateFromIndex((uint)(sortKey + Seed)) });
                ECB.SetComponent(sortKey, spawnedEntity, newTransform);
                ECB.SetComponent(sortKey, spawnedEntity, new ManagedClusterComponent { ClusterParent = clusterEntity });
                
                buffer.Add(new ManagedEntityBuffer { Entity = spawnedEntity });
            }

            RefRW<SpawnPointComponent> spawnPointComponent = SpawnPointLookup.GetRefRW(spawningComponent.SpawnPoint);
            if (spawnPointComponent.IsValid)
            {
                spawnPointComponent.ValueRW.IsSpawning = false;
            }
            ECB.AddComponent<DeathTag>(sortKey, entity);
        }

        private DynamicBuffer<ManagedEntityBuffer> SpawnCluster(int sortKey, Entity clusterEntity, float3 spawnPosition)
        {
            ECB.AddComponent<UpdatePositioningTag>(sortKey, clusterEntity);
            ECB.AddComponent(sortKey, clusterEntity, new FlowFieldComponent { MoveTimer = StartMoveDelay, PathIndex = PathUtility.GetIndex(spawnPosition.xz) });
            ECB.AddComponent(sortKey, clusterEntity, new SpeedComponent { Speed = EnemySpeed });
           
            PathIndex pathIndex = PathUtility.GetIndex(spawnPosition.xz);
            ref PathChunk valuePathChunk = ref PathChunks.Value.PathChunks[ChunkIndexToListIndex[pathIndex.ChunkIndex]];
            float2 direction = PathUtility.ByteToDirection(valuePathChunk.Directions[pathIndex.GridIndex]);
            ECB.AddComponent(sortKey, clusterEntity, new EnemyClusterComponent
            {
                Position = spawnPosition, 
                EnemySize = 0.25f,
                Facing = direction
            });
            
            DynamicBuffer<ManagedEntityBuffer> buffer = ECB.AddBuffer<ManagedEntityBuffer>(sortKey, clusterEntity);
            return buffer;
        }
    }
}
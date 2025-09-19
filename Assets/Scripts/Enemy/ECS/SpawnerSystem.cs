using Random = Unity.Mathematics.Random;
using Gameplay.Turns.ECS;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Pathfinding.ECS;
using Unity.Entities;
using Effects.ECS;
using Pathfinding;
using Unity.Burst;
using Gameplay;

namespace Enemy.ECS
{
    [UpdateAfter(typeof(DeathSystem))]
    public partial struct SpawnerSystem : ISystem
    {
        private ComponentLookup<MovementSpeedComponent> movementSpeedLookup; 
        private ComponentLookup<SimpleDamageComponent> simpleDamageLookup; 
        private ComponentLookup<AttackSpeedComponent> attackSpeedLookup;
        private ComponentLookup<SpawnPointComponent> spawnPointLookup;
        private ComponentLookup<LocalTransform> transformLookup;
        private ComponentLookup<SpeedComponent> speedLookup; 
            
        private EntityQuery spawnerQuery;
        private EntityQuery updateEnemiesQuery;
        private uint seed;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            movementSpeedLookup = SystemAPI.GetComponentLookup<MovementSpeedComponent>(true);
            simpleDamageLookup = SystemAPI.GetComponentLookup<SimpleDamageComponent>(true);
            attackSpeedLookup = SystemAPI.GetComponentLookup<AttackSpeedComponent>(true);
            spawnPointLookup = SystemAPI.GetComponentLookup<SpawnPointComponent>();
            transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            speedLookup = SystemAPI.GetComponentLookup<SpeedComponent>(true);

            spawnerQuery = SystemAPI.QueryBuilder().WithAll<SpawnPointComponent>().Build();
            updateEnemiesQuery = SystemAPI.QueryBuilder().WithAll<TurnIncreaseComponent, UpdateEnemiesTag>().Build();
            
            state.RequireForUpdate<EnemyBossDatabaseTag>();
            state.RequireForUpdate<RandomSeedComponent>();
            state.RequireForUpdate<SpawnPointComponent>();
            state.RequireForUpdate<EnemyDatabaseTag>();
            state.RequireForUpdate<UpdateEnemiesTag>();
            state.RequireForUpdate<PathBlobber>();
        }

        public void OnUpdate(ref SystemState state)
        {
            TurnIncreaseComponent turnIncrease = updateEnemiesQuery.GetSingleton<TurnIncreaseComponent>();
            if (turnIncrease.TurnIncrease <= 0) return;

            if (seed == 0)
            {
                seed = SystemAPI.GetSingleton<RandomSeedComponent>().Seed;
            }

            uint totalSeed = (uint)(seed + turnIncrease.TotalTurn);
            UpdateSpawners(ref state, turnIncrease.TurnIncrease, totalSeed);
            SpawnSpawners(ref state, turnIncrease, totalSeed);
        }

        [BurstCompile]
        private void UpdateSpawners(ref SystemState state, int turnsIncrease, uint seed)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

            Entity enemyDatabase = SystemAPI.GetSingletonEntity<EnemyDatabaseTag>();
            Entity bossDatabase = SystemAPI.GetSingletonEntity<EnemyBossDatabaseTag>();
            NativeArray<EnemyBufferElement> enemyBuffer = SystemAPI.GetBuffer<EnemyBufferElement>(enemyDatabase).AsNativeArray();
            NativeArray<EnemyBossElement> bossBuffer = SystemAPI.GetBuffer<EnemyBossElement>(bossDatabase).AsNativeArray();
            PathBlobber pathBlobber = SystemAPI.GetSingleton<PathBlobber>();
            EntityQueryMask queryMask = state.EntityManager.UniversalQuery.GetEntityQueryMask();
            
            movementSpeedLookup.Update(ref state);
            simpleDamageLookup.Update(ref state);
            attackSpeedLookup.Update(ref state);
            spawnPointLookup.Update(ref state);
            transformLookup.Update(ref state);
            speedLookup.Update(ref state);
            
            state.Dependency = new SpawnEnemiesJob
            {
                ChunkIndexToListIndex = pathBlobber.ChunkIndexToListIndex,
                MovementSpeedLookup = movementSpeedLookup,
                AttackSpeedLookup = attackSpeedLookup,
                SpawnPointLookup = spawnPointLookup,
                DamageLookup = simpleDamageLookup,
                TransformLookup = transformLookup,
                PathChunks = pathBlobber.PathBlob,
                ECB = ecb.AsParallelWriter(),
                TurnIncrease = turnsIncrease,
                EntityQueryMask = queryMask,
                SpeedLookup = speedLookup,
                EnemyBuffer = enemyBuffer,
                BossBuffer = bossBuffer,
                Seed = seed, 
            }.ScheduleParallel(state.Dependency);

            state.Dependency.Complete(); 
            ecb.Playback(state.EntityManager);

            ecb.Dispose();
            enemyBuffer.Dispose();
            bossBuffer.Dispose();
        }
 
        private void SpawnSpawners(ref SystemState state, TurnIncreaseComponent turnIncrease, uint seed)
        {
            int spawnAmount = EnemySpawnHandler.SpawnDataUtility.GetSpawnAmount(turnIncrease.TurnIncrease, turnIncrease.TotalTurn, Random.CreateFromIndex(seed));
            if (spawnAmount <= 0)
            {
                return;
            }
            
            NativeArray<SpawningComponent> possibleSpawns = EnemySpawnHandler.SpawnDataUtility.GetSpawnPointData(turnIncrease.TotalTurn, Random.CreateFromIndex(seed));
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
            NativeArray<bool> shouldSpawns = GetShouldSpawns(spawnAmount);
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
        
        [ReadOnly]
        public ComponentLookup<MovementSpeedComponent> MovementSpeedLookup;
        
        [ReadOnly]
        public ComponentLookup<SpeedComponent> SpeedLookup;
        
        [ReadOnly]
        public ComponentLookup<AttackSpeedComponent> AttackSpeedLookup;
        
        [ReadOnly]
        public ComponentLookup<SimpleDamageComponent> DamageLookup;
        
        [NativeDisableParallelForRestriction]
        public ComponentLookup<SpawnPointComponent> SpawnPointLookup;
        
        [ReadOnly]
        public BlobAssetReference<PathChunkArray> PathChunks;
        
        [ReadOnly]
        public NativeHashMap<int2, int>.ReadOnly ChunkIndexToListIndex;

        [ReadOnly]
        public EntityQueryMask EntityQueryMask;
        
        public EntityCommandBuffer.ParallelWriter ECB;

        public int TurnIncrease;
        public uint Seed;

        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, ref SpawningComponent spawningComponent)
        {
            spawningComponent.Turns -= TurnIncrease;
            if (spawningComponent.Turns > 0) return;
            
            int enemyIndex = spawningComponent.EnemyIndex;
            Entity prefabEntity = enemyIndex >= 100 ? BossBuffer[enemyIndex - 100].EnemyEntity : EnemyBuffer[enemyIndex].EnemyEntity;
            float3 spawnPosition = spawningComponent.Position;
            
            Entity clusterEntity = ECB.CreateEntity(sortKey);
            DynamicBuffer<ManagedEntityBuffer> buffer = SpawnCluster(sortKey, clusterEntity, spawnPosition, enemyIndex, prefabEntity);

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

            ECB.AddComponent<DeathTag>(sortKey, entity);
            
            if (!EntityQueryMask.MatchesIgnoreFilter(spawningComponent.SpawnPoint)) return;
            SpawnPointLookup.GetRefRW(spawningComponent.SpawnPoint).ValueRW.IsSpawning = false;
        }

        private DynamicBuffer<ManagedEntityBuffer> SpawnCluster(int sortKey, Entity clusterEntity, float3 spawnPosition, int enemyIndex, Entity prefabEntity)
        {
            MovementSpeedComponent moveSpeed = MovementSpeedLookup[prefabEntity];
            // This ain't it cheif
            ECB.AddComponent<UpdatePositioningTag>(sortKey, clusterEntity);
            ECB.AddComponent(sortKey, clusterEntity, AttackSpeedLookup[prefabEntity]);
            ECB.AddComponent(sortKey, clusterEntity, DamageLookup[prefabEntity]);
            ECB.AddComponent(sortKey, clusterEntity, SpeedLookup[prefabEntity]);
            ECB.AddComponent(sortKey, clusterEntity, moveSpeed);
            ECB.AddComponent(sortKey, clusterEntity, new FlowFieldComponent { MoveTimer = moveSpeed.Speed, PathIndex = PathUtility.GetIndex(spawnPosition.xz) });

            PathIndex pathIndex = PathUtility.GetIndex(spawnPosition.xz);
            ref PathChunk valuePathChunk = ref PathChunks.Value.PathChunks[ChunkIndexToListIndex[pathIndex.ChunkIndex]];
            float2 direction = PathUtility.ByteToDirection(valuePathChunk.Directions[pathIndex.GridIndex]);
            
            float3 facingPosition = spawnPosition + (math.round(direction) * PathUtility.CELL_SCALE).XyZ();
            PathIndex facingPathIndex = PathUtility.GetIndex(facingPosition.xz);
            PathIndex facingIndex = ChunkIndexToListIndex.ContainsKey(facingPathIndex.ChunkIndex) ? facingPathIndex : pathIndex;

            ECB.AddComponent(sortKey, clusterEntity, new EnemyClusterComponent
            {
                TargetPathIndex = facingIndex,
                Position = spawnPosition, 
                EnemyType = enemyIndex,
                Facing = direction,
                EnemySize = 0.25f,
            });
            
            DynamicBuffer<ManagedEntityBuffer> buffer = ECB.AddBuffer<ManagedEntityBuffer>(sortKey, clusterEntity);
            return buffer;
        }
    }
}
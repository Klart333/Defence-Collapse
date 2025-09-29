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

        private DynamicBuffer<EnemyBufferElement> enemyBuffer;
        private DynamicBuffer<EnemyBossElement> bossBuffer;
        
        private EntityQuery updateEnemiesQuery;
        private uint seed;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            movementSpeedLookup = SystemAPI.GetComponentLookup<MovementSpeedComponent>(true);
            simpleDamageLookup = SystemAPI.GetComponentLookup<SimpleDamageComponent>(true);
            attackSpeedLookup = SystemAPI.GetComponentLookup<AttackSpeedComponent>(true);
            spawnPointLookup = SystemAPI.GetComponentLookup<SpawnPointComponent>();
            transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            speedLookup = SystemAPI.GetComponentLookup<SpeedComponent>(true);

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
            
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            uint totalSeed = (uint)(seed + turnIncrease.TotalTurn);
            SpawnSpawners(ref state, turnIncrease, totalSeed, ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged));
            UpdateSpawners(ref state, turnIncrease.TurnIncrease, totalSeed, ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged));
        }

        [BurstCompile]
        private void UpdateSpawners(ref SystemState state, int turnsIncrease, uint seed, EntityCommandBuffer ecb)
        {
            PathBlobber pathBlobber = SystemAPI.GetSingleton<PathBlobber>();
            EntityQueryMask queryMask = state.EntityManager.UniversalQuery.GetEntityQueryMask();
            
            Entity bossDatabase = SystemAPI.GetSingletonEntity<EnemyBossDatabaseTag>();
            Entity enemyDatabase = SystemAPI.GetSingletonEntity<EnemyDatabaseTag>();
            enemyBuffer = SystemAPI.GetBuffer<EnemyBufferElement>(enemyDatabase);
            bossBuffer = SystemAPI.GetBuffer<EnemyBossElement>(bossDatabase);
            
            movementSpeedLookup.Update(ref state);
            simpleDamageLookup.Update(ref state);
            attackSpeedLookup.Update(ref state);
            spawnPointLookup.Update(ref state);
            transformLookup.Update(ref state);
            speedLookup.Update(ref state);
            
            new SpawnEnemiesJob
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
            }.ScheduleParallel();
        }
 
        private void SpawnSpawners(ref SystemState state, TurnIncreaseComponent turnIncrease, uint seed, EntityCommandBuffer ecb)
        {
            NativeArray<SpawningComponent> possibleSpawns = EnemySpawnHandler.SpawnDataUtility.GetSpawnPointData(turnIncrease.TotalTurn, Random.CreateFromIndex(seed));
            new SpawnSpawningJob
            {
                ECB = ecb.AsParallelWriter(),
                PossibleSpawns = possibleSpawns
            }.ScheduleParallel();
        }
        
        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
    
    [BurstCompile]
    public partial struct SpawnSpawningJob : IJobEntity
    {
        [ReadOnly, DeallocateOnJobCompletion]
        public NativeArray<SpawningComponent> PossibleSpawns;
        
        public EntityCommandBuffer.ParallelWriter ECB;
        
        [BurstCompile]
        public void Execute([EntityIndexInChunk] int sortKey, Entity entity, ref SpawnPointComponent spawnPointComponent)
        {
            if (spawnPointComponent.IsSpawning)
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
        public DynamicBuffer<EnemyBufferElement> EnemyBuffer;
        
        [ReadOnly]
        public DynamicBuffer<EnemyBossElement> BossBuffer;

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

            float3 spawnPosition = spawningComponent.Position;
            PathIndex pathIndex = PathUtility.GetIndex(spawnPosition.xz);
            ref PathChunk valuePathChunk = ref PathChunks.Value.PathChunks[ChunkIndexToListIndex[pathIndex.ChunkIndex]];
            if (valuePathChunk.IndexOccupied[pathIndex.GridIndex]) return;
            
            valuePathChunk.IndexOccupied[pathIndex.GridIndex] = true;
            
            int enemyIndex = spawningComponent.EnemyIndex;
            Entity prefabEntity = enemyIndex >= 100 ? BossBuffer[enemyIndex - 100].EnemyEntity : EnemyBuffer[enemyIndex].EnemyEntity;
            
            LocalTransform prefabTransform = TransformLookup[prefabEntity];
            float2 direction = PathUtility.ByteToDirection(valuePathChunk.Directions[pathIndex.GridIndex]);

            Entity clusterEntity = ECB.CreateEntity(sortKey);
            DynamicBuffer<ManagedEntityBuffer> buffer = SpawnCluster(sortKey, clusterEntity, spawnPosition, prefabTransform.Scale, enemyIndex, prefabEntity, pathIndex, direction);

            for (int i = 0; i < spawningComponent.Amount; i++)
            {
                Entity spawnedEntity = ECB.Instantiate(sortKey, prefabEntity);
            
                LocalTransform newTransform = LocalTransform.FromPositionRotationScale(spawnPosition, quaternion.identity, prefabTransform.Scale);
                ECB.SetComponent(sortKey, spawnedEntity, new RandomComponent { Random = Random.CreateFromIndex((uint)(sortKey + Seed + i + 1)) });
                ECB.SetComponent(sortKey, spawnedEntity, new ManagedClusterComponent { ClusterParent = clusterEntity });
                ECB.SetComponent(sortKey, spawnedEntity, newTransform);
                
                buffer.Add(new ManagedEntityBuffer { Entity = spawnedEntity });
            }

            ECB.AddComponent<DeathTag>(sortKey, entity);
            
            if (!EntityQueryMask.MatchesIgnoreFilter(spawningComponent.SpawnPoint)) return;
            SpawnPointLookup.GetRefRW(spawningComponent.SpawnPoint).ValueRW.IsSpawning = false;
        }

        private DynamicBuffer<ManagedEntityBuffer> SpawnCluster(int sortKey, Entity clusterEntity, float3 spawnPosition, float enemySize, int enemyIndex, Entity prefabEntity, PathIndex pathIndex, float2 direction)
        {
            MovementSpeedComponent moveSpeed = MovementSpeedLookup[prefabEntity];
            
            // This ain't it cheif
            ECB.AddComponent(sortKey, clusterEntity, AttackSpeedLookup[prefabEntity]);
            ECB.AddComponent(sortKey, clusterEntity, DamageLookup[prefabEntity]);
            ECB.AddComponent(sortKey, clusterEntity, SpeedLookup[prefabEntity]);
            ECB.AddComponent(sortKey, clusterEntity, moveSpeed);
            ECB.AddComponent(sortKey, clusterEntity, new FlowFieldComponent { MoveTimer = moveSpeed.Speed, PathIndex = PathUtility.GetIndex(spawnPosition.xz) });
            ECB.AddComponent(sortKey, clusterEntity, new RandomComponent { Random = Random.CreateFromIndex((uint)(sortKey + Seed)) });
            
            float3 facingPosition = spawnPosition + (math.round(direction) * PathUtility.CELL_SCALE).XyZ();
            PathIndex facingPathIndex = PathUtility.GetIndex(facingPosition.xz);
            PathIndex facingIndex = ChunkIndexToListIndex.ContainsKey(facingPathIndex.ChunkIndex) ? facingPathIndex : pathIndex;

            ECB.AddComponent(sortKey, clusterEntity, new EnemyClusterComponent
            {
                TargetPathIndex = facingIndex,
                Position = spawnPosition, 
                EnemyType = enemyIndex,
                EnemySize = enemySize,
                Facing = direction,
            });
            
            ECB.AddComponent(sortKey, clusterEntity, new UpdatePositioningComponent
            {
                CurrentTile = pathIndex,
                PreviousTile = new PathIndex(0, -1),
            });
            
            
            DynamicBuffer<ManagedEntityBuffer> buffer = ECB.AddBuffer<ManagedEntityBuffer>(sortKey, clusterEntity);
            return buffer;
        }
    }
}
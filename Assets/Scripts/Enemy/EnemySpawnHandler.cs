using System;
using System.Collections.Generic;
using Effects.ECS;
using Gameplay;
using Sirenix.OdinInspector;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using WaveFunctionCollapse;
using Random = System.Random;

namespace Enemy
{
    public class EnemySpawnHandler : MonoBehaviour
    {
        [Title("References")]
        [SerializeField]
        private GroundGenerator groundGenerator;
        
        [Title("Spawn Point")]
        [SerializeField]
        private EnemySpawnPoint spawnPointPrefab;

        [SerializeField]
        private AnimationCurve shouldSpawnCurve;

        [SerializeField]
        private AnimationCurve minimumSpawnsCurve;
        
        [SerializeField]
        private AnimationCurve maximumSpawnsCurve;
        
        private readonly Dictionary<int3, List<EnemySpawnPoint>>  spawnPoints = new Dictionary<int3, List<EnemySpawnPoint>>();
        private Random random;
        
        private EntityManager entityManager;
        private Entity waveCountEntity;
        
        public Dictionary<int3, List<EnemySpawnPoint>> SpawnPoints => spawnPoints;
        
        public int WaveCount { get; private set; }

        private void OnEnable()
        {
            groundGenerator.OnChunkGenerated += OnChunkUnlocked;
            Events.OnWaveStarted += OnWaveStarted;
            
            random = new Random(GameManager.Instance?.Seed ?? -1);
            
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            waveCountEntity = entityManager.CreateEntity();
            entityManager.AddComponent<WaveCountComponent>(waveCountEntity);
        }

        private void OnDisable()
        {
            groundGenerator.OnChunkGenerated -= OnChunkUnlocked;
            Events.OnWaveStarted -= OnWaveStarted;
        }

        private void OnWaveStarted()
        {
            WaveCount++;
            entityManager.SetComponentData(waveCountEntity, new WaveCountComponent { Value = WaveCount });
        }

        private void OnChunkUnlocked(Chunk chunk)
        {
            if (spawnPoints.TryGetValue(chunk.ChunkIndex, out List<EnemySpawnPoint> spawnPointToRemove))
            {
                foreach (EnemySpawnPoint point in spawnPointToRemove)
                {
                    point.gameObject.SetActive(false);
                }
                spawnPoints.Remove(chunk.ChunkIndex);
            }
        }

        public void SetEnemySpawn(Vector3 pos, int3 chunkIndex, int difficulty)
        {
            EnemySpawnPoint spawned = spawnPointPrefab.GetAtPosAndRot<EnemySpawnPoint>(pos, Quaternion.identity);
            spawned.BaseDifficulty = difficulty;
            spawned.EnemySpawnHandler = this;
            if (spawnPoints.TryGetValue(chunkIndex, out List<EnemySpawnPoint> list))
            {
                list.Add(spawned);
            }
            else
            {
                spawnPoints.Add(chunkIndex, new List<EnemySpawnPoint> { spawned });
            }
        }
        
        public int GetMaxSpawns(int difficulty) => (int)System.Math.Round(maximumSpawnsCurve.Evaluate(difficulty), MidpointRounding.AwayFromZero);

        public bool ShouldSetSpawnPoint(int3 chunkIndex, out int difficulty)
        {
            int distance = Mathf.Abs(chunkIndex.x) + Mathf.Abs(chunkIndex.z);
            difficulty = distance;

            return random.NextDouble() <= shouldSpawnCurve.Evaluate(difficulty);
        }

        public bool ShouldForceSpawnPoint(int index, int max, int amountSpawned, int difficulty)
        {
            int minimum = (int)System.Math.Round(minimumSpawnsCurve.Evaluate(difficulty), MidpointRounding.AwayFromZero);
            int pointsLeft = max - index;
            return pointsLeft + amountSpawned < minimum;
        }
    }
}
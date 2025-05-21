using System.Collections.Generic;
using Random = System.Random;
using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using Unity.Mathematics;
using Unity.Entities;
using Effects.ECS;
using UnityEngine;
using Gameplay;
using System;

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

        [Title("UI Display")]
        [SerializeField]
        private UIEnemySpawnPointDisplay displayPrefab;
        
        private readonly Dictionary<int3, List<EnemySpawnPoint>>  spawnPoints = new Dictionary<int3, List<EnemySpawnPoint>>();
        private readonly Dictionary<int3, List<UIEnemySpawnPointDisplay>> displays = new Dictionary<int3, List<UIEnemySpawnPointDisplay>>();
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
            
            if (displays.TryGetValue(chunk.ChunkIndex, out List<UIEnemySpawnPointDisplay> displaysToRemove))
            {
                foreach (UIEnemySpawnPointDisplay display in displaysToRemove)
                {
                    display.gameObject.SetActive(false);
                }
                displays.Remove(chunk.ChunkIndex);
            }
        }

        public void SetEnemySpawn(Vector3 pos, int3 chunkIndex, int difficulty)
        {
            EnemySpawnPoint spawned = spawnPointPrefab.GetAtPosAndRot<EnemySpawnPoint>(pos, Quaternion.identity);
            spawned.BaseDifficulty = difficulty;
            spawned.EnemySpawnHandler = this;
            if (spawnPoints.TryGetValue(chunkIndex, out List<EnemySpawnPoint> list)) list.Add(spawned);
            else spawnPoints.Add(chunkIndex, new List<EnemySpawnPoint> { spawned });

            UIEnemySpawnPointDisplay display = displayPrefab.GetAtPosAndRot<UIEnemySpawnPointDisplay>(pos, Quaternion.identity);
            display.DisplayPoint(pos, spawned);
            if (displays.TryGetValue(chunkIndex, out List<UIEnemySpawnPointDisplay> list2)) list2.Add(display);
            else displays.Add(chunkIndex, new List<UIEnemySpawnPointDisplay> { display });

        }
        
        public int GetMaxSpawns(int difficulty) => (int)Math.Round(maximumSpawnsCurve.Evaluate(difficulty), MidpointRounding.AwayFromZero);

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
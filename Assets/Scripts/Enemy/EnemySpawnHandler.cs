using System;
using System.Collections.Generic;
using Gameplay;
using Sirenix.OdinInspector;
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
        
        private readonly Dictionary<int3, List<EnemySpawnPoint>>  spawnPoints = new Dictionary<int3, List<EnemySpawnPoint>>();
        private Random random;

        private void OnEnable()
        {
            groundGenerator.OnChunkGenerated += OnChunkUnlocked;
            
            random = new Random(GameManager.Instance?.Seed ?? -1);
        }

        private void OnDisable()
        {
            groundGenerator.OnChunkGenerated -= OnChunkUnlocked;
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
            if (spawnPoints.TryGetValue(chunkIndex, out List<EnemySpawnPoint> list))
            {
                list.Add(spawned);
            }
            else
            {
                spawnPoints.Add(chunkIndex, new List<EnemySpawnPoint> { spawned });
            }
        }

        public bool ShouldSetSpawnPoint(int3 chunkIndex, out int difficulty)
        {
            int distance = chunkIndex.x + chunkIndex.y;
            difficulty = distance;

            return random.NextDouble() <= shouldSpawnCurve.Evaluate(difficulty);
        }
    }
}
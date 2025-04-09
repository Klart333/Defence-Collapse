using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using WaveFunctionCollapse;

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
        
        private readonly Dictionary<int3, List<EnemySpawnPoint>>  spawnPoints = new Dictionary<int3, List<EnemySpawnPoint>>();

        private void OnEnable()
        {
            groundGenerator.OnChunkGenerated += OnChunkUnlocked;
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

        public void SetEnemySpawn(Vector3 pos, int3 chunkIndex)
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
    }
}
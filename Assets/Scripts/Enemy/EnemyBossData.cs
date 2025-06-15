using System.Collections.Generic;
using Random = System.Random;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

namespace Enemy
{
    [InlineEditor, CreateAssetMenu(fileName = "Enemy Boss Data", menuName = "Enemy/Boss/Enemy Boss Data", order = 0)]
    public class EnemyBossData : ScriptableObject
    {
        [Title("Bosses")]
        [SerializeField]
        private EnemyUtility enemyUtility;
        
        [Title("Spawn Frequency")]
        [SerializeField]
        private int bossSpawnFrequency = 10;

        [SerializeField]
        private float frequencyDecrease = 0.5f;

        [SerializeField]
        private float bossPerSpawnPointLevel = 0.1f;
        
        public int BossSpawnFrequency => bossSpawnFrequency;
        public float FrequencyDecrease => frequencyDecrease;

        public List<SpawnPointInfo> GetBossSpawnPoints(List<Vector3> spawnPoints, int totalLevels, int seed, float roundDuration)
        {
            roundDuration = Mathf.Max(5, roundDuration - 10);
            List<SpawnPointInfo> bossSpawnPoints = new List<SpawnPointInfo>();
            int bossAmount = 1 + Mathf.FloorToInt(totalLevels * bossPerSpawnPointLevel);
            
            Random random = new Random(seed);
            spawnPoints.Shuffle(random);
            while (bossAmount > 0)
            {
                int amount = Mathf.Max(1, Mathf.RoundToInt((float)bossAmount / spawnPoints.Count));
                bossAmount -= amount;

                float spawnRate = roundDuration / amount;
                SpawnPointInfo info = new SpawnPointInfo
                {
                    Amount = amount,
                    EnemyIndex = 100 + random.Next(enemyUtility.Enemies.Count),
                    SpawnRate = spawnRate,
                    Timer = (float)random.NextDouble() * spawnRate,
                    Position = spawnPoints[^1],
                };
                
                spawnPoints.RemoveAt(spawnPoints.Count - 1);
                bossSpawnPoints.Add(info);
            }
            
            return bossSpawnPoints;
        }

        public struct SpawnPointInfo
        {
            public float SpawnRate;
            public int EnemyIndex;
            public float Timer;
            public int Amount;
            public Vector3 Position;
        }
    }
}
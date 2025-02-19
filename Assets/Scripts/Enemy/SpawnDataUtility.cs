using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Enemy
{
    [CreateAssetMenu(fileName = "SpawnDataUtility", menuName = "Enemy/Spawn Data Utility"), InlineEditor] 
    class SpawnDataUtility : ScriptableObject
    {
        [SerializeField]
        private EnemyUtility enemyUtility;
        
        public SpawnPointComponent GetSpawnPointData(int spawnPointLevel, int waveLevel)
        {
            float credits = 5 + Mathf.Pow((spawnPointLevel + waveLevel) / 2.0f, 1.5f) * Random.Range(0.9f, 1.1f);
            List<int> possibleEnemies = new List<int>();

            for (int i = 0; i < enemyUtility.Enemies.Count; i++)
            {
                if (credits > enemyUtility.Enemies[i].EnemyData.UnlockedThreshold)
                {
                    possibleEnemies.Add(i);
                }
            }
            
            return new SpawnPointComponent
            {
                EnemyIndex = 0,
                SpawnRate = 0,
                Amount = 1000,
                Timer = 1,
            };
        }
    }
}
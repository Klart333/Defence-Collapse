using Random = Unity.Mathematics.Random;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Collections;
using UnityEngine;
using Enemy.ECS;
using Unity.Mathematics;

namespace Enemy
{
    [CreateAssetMenu(fileName = "SpawnDataUtility", menuName = "Enemy/Spawn Data Utility"), InlineEditor] 
    public class SpawnDataUtility : ScriptableObject
    {
        [Title("Data")]
        [SerializeField]
        private EnemyUtility enemyUtility;

        [Title("Settings")]
        [SerializeField]
        private float startCredits = 5;

        [SerializeField]
        private float creditBaseMultiplier = 0.5f;
        
        [SerializeField]
        private float creditExponent = 1.5f;
        
        /// <returns>Returns a TempJob allocated array</returns>
        public NativeArray<SpawningComponent> GetSpawnPointData(int turns, Random random)
        {
            float baseCredits = turns * creditBaseMultiplier;
            float credits = startCredits + Mathf.Pow(baseCredits, creditExponent) * random.NextFloat(0.9f, 1.1f);
            List<int> possibleEnemies = new List<int>();

            for (int i = 0; i < enemyUtility.Enemies.Count; i++)
            {
                if (credits > enemyUtility.Enemies[i].EnemyData.UnlockedThreshold)
                {
                    possibleEnemies.Add(i);
                }
            }

            NativeArray<SpawningComponent> possibleSpawns = new NativeArray<SpawningComponent>(possibleEnemies.Count, Allocator.TempJob);

            for (int i = 0; i < possibleEnemies.Count; i++)
            {
                possibleSpawns[i] = new SpawningComponent
                {
                    EnemyIndex = possibleEnemies[i],
                    Amount = Mathf.FloorToInt(credits / enemyUtility.Enemies[possibleEnemies[i]].EnemyData.CreditCost),
                    Turns = random.NextInt(3, 5),
                };
            }

            return possibleSpawns; 
        }

        public int GetSpawnAmount(int turnIncrease, int totalTurn, Random random)
        {
            int attempts = (int)math.ceil(totalTurn / 10.0f) * turnIncrease;
            int result = 0;
            for (int i = 0; i < attempts; i++)
            {
                float value = random.NextFloat();
                float threshold = 1.0f - (1.0f / (0.02f * (totalTurn + 50.0f)));

                if (value <= threshold)
                {
                    result++;
                }
            }
            return result;
        }
    }
}
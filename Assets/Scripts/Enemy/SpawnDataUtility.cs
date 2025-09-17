using Random = Unity.Mathematics.Random;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;
using Enemy.ECS;

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

#if UNITY_EDITOR
        [Title("Debug")]
        [SerializeField]
        private bool sendOnlyOneWave;
#endif
        
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
                    Turns = random.NextInt(4, 10),
                };
            }

            return possibleSpawns; 
        }

        public int GetSpawnAmount(int turnIncrease, int totalTurn, Random random)
        {
            if (totalTurn == 1)
            {
                return 1;
            }

#if UNITY_EDITOR
            if (sendOnlyOneWave)
            {
                return 0;
            }
#endif
            
            int attempts = (int)math.ceil(0.05f * totalTurn + 0.0004f * totalTurn * totalTurn) * turnIncrease; // x/20 + 0.0004x^2
            int result = 0;
            for (int i = 0; i < attempts; i++)
            {
                float value = random.NextFloat();
                float threshold = 1.0f - (1.0f / (0.01f * (totalTurn + 100.0f)));

                if (value <= threshold)
                {
                    result++;
                }
            }
            return result;
        }
    }
}
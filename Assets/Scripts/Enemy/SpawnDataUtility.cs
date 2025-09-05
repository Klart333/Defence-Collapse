using System.Collections.Generic;
using Enemy.ECS;
using Sirenix.OdinInspector;
using Unity.Collections;
using UnityEngine;

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
        
        /// <summary>
        /// </summary>
        /// <param name="turns"></param>
        /// <returns>Returns a TempJob allocated array</returns>
        public NativeArray<SpawningComponent> GetSpawnPointData(int turns)
        {
            float baseCredits = turns * creditBaseMultiplier;
            float credits = startCredits + Mathf.Pow(baseCredits, creditExponent) * Random.Range(0.9f, 1.1f);
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
                    Turns = 3,
                };
            }

            return possibleSpawns; 
        }
    }
}
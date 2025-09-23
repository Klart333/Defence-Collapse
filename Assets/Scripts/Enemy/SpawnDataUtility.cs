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

        [Title("Curves")]
        [SerializeField]
        private AnimationCurve maxTurnsCurve;
        
        [SerializeField]
        private AnimationCurve minTurnsCurve;
        
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
                    Turns = random.NextInt((int)minTurnsCurve.Evaluate(turns), (int)maxTurnsCurve.Evaluate(turns)),
                };
            }

            return possibleSpawns; 
        }
    }
}
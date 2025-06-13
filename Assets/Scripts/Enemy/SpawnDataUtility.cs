using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Enemy
{
    [CreateAssetMenu(fileName = "SpawnDataUtility", menuName = "Enemy/Spawn Data Utility"), InlineEditor] 
    class SpawnDataUtility : ScriptableObject
    {
        [Title("Data")]
        [SerializeField]
        private EnemyUtility enemyUtility;

        [Title("Settings")]
        [SerializeField]
        private AnimationCurve targetWaveDuration;

        [SerializeField]
        private float startCredits = 5;

        [SerializeField]
        private float creditBaseMultiplier = 0.5f;
        
        [SerializeField]
        private float creditExponent = 1.5f;
        
        public AnimationCurve TargetWaveDuration => targetWaveDuration;
        
        public SpawnPointComponent GetSpawnPointData(int spawnPointLevel, int waveLevel)
        {
            float combinedWaveLevel = (spawnPointLevel + waveLevel) * creditBaseMultiplier;
            float credits = startCredits + Mathf.Pow(combinedWaveLevel, creditExponent) * Random.Range(0.9f, 1.1f);
            List<int> possibleEnemies = new List<int>();

            //Debug.Log("Total Credits: " + credits);
            for (int i = 0; i < enemyUtility.Enemies.Count; i++)
            {
                if (credits > enemyUtility.Enemies[i].EnemyData.UnlockedThreshold)
                {
                    possibleEnemies.Add(i);
                }
            }
            
            int enemyIndex = Random.Range(0, possibleEnemies.Count);
            int amount = Mathf.FloorToInt(credits / enemyUtility.Enemies[enemyIndex].EnemyData.CreditCost);
            float spawnRate = (targetWaveDuration.Evaluate(combinedWaveLevel) / amount) * Random.Range(0.75f, 1.5f);
            
            return new SpawnPointComponent
            {
                EnemyIndex = enemyIndex,
                SpawnRate = spawnRate,
                Amount = amount,
                Timer = 1,
            };
        }
    }
}
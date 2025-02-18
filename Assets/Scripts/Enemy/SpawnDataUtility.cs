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
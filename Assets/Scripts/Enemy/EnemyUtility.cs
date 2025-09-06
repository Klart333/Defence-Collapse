using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Enemy
 {
    [CreateAssetMenu(fileName = "EnemyUtility", menuName = "Enemy/Enemy Utility", order = 0), InlineEditor]
    public class EnemyUtility : ScriptableObject
    {
        public List<Enemy> Enemies;

        public EnemyData GetEnemy(int index)
        {
            return Enemies[index].EnemyData;
        }
    }
}
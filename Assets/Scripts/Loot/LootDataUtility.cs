using Sirenix.OdinInspector;
using UnityEngine;

namespace Loot
{
    [CreateAssetMenu(fileName = "LootDataUtility", menuName = "Loot/LootDataUtility", order = 0)]
    [InlineEditor]
    public class LootDataUtility : ScriptableObject
    {
        [SerializeField]
        private LootData[] lootDatas;
        
        public LootData[] LootDatas => lootDatas;
    }
}
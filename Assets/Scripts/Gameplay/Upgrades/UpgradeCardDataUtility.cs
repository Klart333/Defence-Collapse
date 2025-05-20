using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Upgrades
{
    [CreateAssetMenu(fileName = "Upgrade Card Utility", menuName = "Upgrade/Upgrade Card Utility", order = 0)]
    public class UpgradeCardDataUtility : ScriptableObject
    {
        [SerializeField]
        private UpgradeCardData[] upgradeCards;
        
        public UpgradeCardData[] UpgradeCards => upgradeCards;
        
        public List<UpgradeCardData> GetRandomData(int seed, int amount)
        {
            List<UpgradeCardData> availableDatas = new List<UpgradeCardData>(upgradeCards);
            List<UpgradeCardData> result = new List<UpgradeCardData>();
            System.Random random = new System.Random(seed);
            for (int i = 0; i < amount; i++)
            {
                int index = random.Next(0, availableDatas.Count);
                result.Add(availableDatas[index]);
                availableDatas.RemoveAt(index);
            }
            return result;
        }
    }
}
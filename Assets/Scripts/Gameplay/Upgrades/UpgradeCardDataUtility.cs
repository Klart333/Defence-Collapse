using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using System.Linq;
#endif

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
        
        #if UNITY_EDITOR
        [Button]
        public void FindAllUpgradeCardDatasInFolder()
        {
            // Get the path of the current scriptable object
            string currentPath = AssetDatabase.GetAssetPath(this);
            string directoryPath = Path.GetDirectoryName(currentPath);
    
            if (string.IsNullOrEmpty(directoryPath))
            {
                Debug.LogError("Could not determine the folder path of this utility object.");
                return;
            }
    
            // Find all UpgradeCardData assets in this folder and subfolders
            string[] guids = AssetDatabase.FindAssets("t:UpgradeCardData", new[] { directoryPath });
    
            List<UpgradeCardData> upgradeCardDatas = new List<UpgradeCardData>();
    
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                UpgradeCardData cardData = AssetDatabase.LoadAssetAtPath<UpgradeCardData>(assetPath);
                if (cardData != null)
                {
                    upgradeCardDatas.Add(cardData);
                }
            }
    
            upgradeCards = upgradeCardDatas.ToArray();
            // Mark the object as dirty so changes will be saved
            EditorUtility.SetDirty(this);
            Debug.Log($"Found {upgradeCardDatas.Count} UpgradeCardData assets in {directoryPath} and its subfolders.");
        }
#endif
    }
}
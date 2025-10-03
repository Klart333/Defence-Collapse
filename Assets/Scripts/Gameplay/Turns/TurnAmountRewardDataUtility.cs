using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Gameplay.Turns
{
    [CreateAssetMenu(fileName = "Turn Amount Reward Utility", menuName = "Reward/Turn Amount/Turn Amount Reward Utility", order = 0)]
    public class TurnAmountRewardDataUtility : ScriptableObject 
    {
        [SerializeField]
        private TurnAmountRewardData[] rewardDatas;

        public TurnAmountRewardData GetRewardData(int index) => rewardDatas[index];
        
        public bool TryGetIndex(TurnAmountRewardData unlockedReward, out int index)
        {
            for (int i = 0; i < rewardDatas.Length; i++)
            {
                if (rewardDatas.Equals(unlockedReward))
                {
                    index = i;
                    return true;
                }
            }

            index = -1; 
            return false;
        }
    
#if UNITY_EDITOR
        [Button]
        public void FindAllRewardData()
        {
            // Get the path of the current scriptable object
            string currentPath = AssetDatabase.GetAssetPath(this);
            string directoryPath = Path.GetDirectoryName(currentPath);
    
            if (string.IsNullOrEmpty(directoryPath))
            {
                Debug.LogError("Could not determine the folder path of this utility object.");
                return;
            }
    
            // Find all TurnAmountRewardData assets in this folder and subfolders
            string[] guids = AssetDatabase.FindAssets("t:TurnAmountRewardData", new[] { directoryPath });
    
            List<TurnAmountRewardData> upgradeCardDatas = new List<TurnAmountRewardData>();
    
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                TurnAmountRewardData cardData = AssetDatabase.LoadAssetAtPath<TurnAmountRewardData>(assetPath);
                if (cardData != null)
                {
                    upgradeCardDatas.Add(cardData);
                }
            }
    
            rewardDatas = upgradeCardDatas.ToArray();
            // Mark the object as dirty so changes will be saved
            EditorUtility.SetDirty(this);
            Debug.Log($"Found {upgradeCardDatas.Count} RewardData assets in {directoryPath} and its subfolders.");
        }
#endif
    }
}
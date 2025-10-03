using Gameplay.Turns;
using MessagePack;
using UnityEngine;

namespace Saving
{
    public class TurnAmountSaveLoad
    {
        private const string SaveFileName = "TurnAmountSaveData";
        
        private ISaveSystem saveSystem;
        
        public TurnAmountSaveLoad(ISaveSystem saveSystem)
        {
            this.saveSystem = saveSystem;    
        }
        
        public void SaveTurnAmountData(int maxTurn, TurnAmountRewardData[] unlockedRewards, int[] rewardLevels, TurnAmountRewardDataUtility rewardDataUtility)
        {
            int[] unlockedRewardIndexes = new int[unlockedRewards.Length];
            for (int i = 0; i < unlockedRewardIndexes.Length; i++)
            {
                if (rewardDataUtility.TryGetIndex(unlockedRewards[i], out int index))
                {
                    unlockedRewardIndexes[i] = index;
                }
                else
                {
                    Debug.LogError($"Could not find {unlockedRewards[i]} in utility");
                }
            }
            
            TurnAmountSaveData saveData = new TurnAmountSaveData
            {
                RewardsUnlocked = unlockedRewardIndexes,
                RewardLevels = rewardLevels,
                MaxTurn = maxTurn,
            };

            saveSystem.SaveData(saveData, SaveFileName);
        }

        public TurnAmountSaveData LoadTurnData()
        {
            TurnAmountSaveData saveData = saveSystem.LoadData(SaveFileName, new TurnAmountSaveData
            {
                MaxTurn = 5,
                RewardsUnlocked = new int[1],
                RewardLevels = new int[1],
            });
            return saveData;
        }
    }
    
    [MessagePackObject]
    [System.Serializable]
    public struct TurnAmountSaveData
    {
        [Key(7)]
        public int MaxTurn;

        [Key(8)]
        public int[] RewardsUnlocked;

        [Key(9)]
        public int[] RewardLevels;
    }
}
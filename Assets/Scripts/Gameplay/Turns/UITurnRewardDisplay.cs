using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UI;

namespace Gameplay.Turns
{
    public class UITurnRewardDisplay : MonoBehaviour
    {
        [Title("Setup")]
        [SerializeField]
        private PooledText textPrefab;

        [SerializeField]
        private RectTransform textContainer;
        
        private List<PooledText> spawnedTexts = new List<PooledText>();
        
        public void Display(int turnAmount, TurnAmountRewardData[] rewardDatas, int[] rewardLevels)
        {
            if (spawnedTexts.Count > rewardDatas.Length)
            {
                RemoveTexts(spawnedTexts.Count - rewardDatas.Length);
            }
            else if (spawnedTexts.Count < rewardDatas.Length)
            {
                AddTexts(rewardDatas.Length - spawnedTexts.Count);
            }

            for (int i = 0; i < rewardDatas.Length; i++)
            {
                if (rewardDatas[i].RewardEffect is ITextEffect textEffect)
                {
                    textEffect.SetText(spawnedTexts[i].Text, turnAmount, rewardLevels[i]);
                }
            }
        }

        private void RemoveTexts(int amountToRemove)
        {
            for (int i = 0; i < amountToRemove; i++)
            {
                spawnedTexts[^1].gameObject.SetActive(false);
                spawnedTexts.RemoveAt(spawnedTexts.Count - 1);
            }
        }
        
        private void AddTexts(int amountToAdd)
        {
            for (int i = 0; i < amountToAdd; i++)
            {
                PooledText spawned = textPrefab.Get<PooledText>(textContainer);
                spawnedTexts.Add(spawned);
            }
        }
    }
}
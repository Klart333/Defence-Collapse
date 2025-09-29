using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Collections;
using UnityEngine;
using System;
using Buildings.District;
using Gameplay.Event;

#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

namespace Gameplay.Upgrades
{
    [InlineEditor, CreateAssetMenu(fileName = "Upgrade Card Utility", menuName = "Upgrade/Upgrade Card Utility", order = 0)]
    public class UpgradeCardDataUtility : ScriptableObject
    {
        [SerializeField]
        private UpgradeCardData[] upgradeCards;

        [SerializeField]
        private TowerData[] startingDistricts;

        private List<UpgradeCardData.UpgradeCardInstance> upgradeCardInstances = new List<UpgradeCardData.UpgradeCardInstance>();
        
        private HashSet<DistrictType> unlockedDistricts = new HashSet<DistrictType>();
        
        public void InitializeUpgrades()
        {
            upgradeCardInstances.Clear();
            foreach (TowerData startingDistrict in startingDistricts)
            {
                unlockedDistricts.Add(startingDistrict.DistrictType);
            }
            
            for (int i = 0; i < upgradeCards.Length; i++)
            {
                upgradeCardInstances.Add(upgradeCards[i].GetUpgradeCardInstance());
            }
        }
        
        public List<UpgradeCardData.UpgradeCardInstance> GetRandomData(int seed, int amount)
        {
            List<UpgradeCardData.UpgradeCardInstance> availableUpgrades = new List<UpgradeCardData.UpgradeCardInstance>(upgradeCardInstances);
            List<UpgradeCardData.UpgradeCardInstance> result = new List<UpgradeCardData.UpgradeCardInstance>();
            
            System.Random random = new System.Random(seed);
            for (int i = 0; i < amount; i++)
            {
                float totalWeight = 0;
                for (int j = availableUpgrades.Count - 1; j >= 0; j--)
                {
                    bool isDistrict = CategoryTypeUtility.GetDistrictType(availableUpgrades[j].AppliedCategories, out DistrictType districtType);
                    bool unlocked = !isDistrict 
                                    || !availableUpgrades[j].WeightStrategy.HasFlag(WeightStrategy.LockToDistrictType)
                                    || unlockedDistricts.Contains(districtType); 
                    
                    if (availableUpgrades[j].Weight <= 0 || !unlocked)
                    {
                        availableUpgrades.RemoveAt(j);
                        continue;
                    }
                    
                    totalWeight += availableUpgrades[j].Weight;
                }
                
                float randomValue = (float)random.NextDouble() * totalWeight;
                for (int j = availableUpgrades.Count - 1; j >= 0; j--)
                {
                    if (randomValue <= availableUpgrades[j].Weight)
                    {
                        result.Add(availableUpgrades[j]);
                        availableUpgrades.RemoveAtSwapBack(j);
                        break;
                    }                    
                    randomValue -= availableUpgrades[j].Weight;
                }
            }
            return result;
        }
        
        public void StartObserving()
        {
            Events.OnDistrictBuilt += OnDistrictBuilt;
            Events.OnUpgradeCardPicked += OnUpgradePicked;
            Events.OnDistrictUnlocked += OnDistrictUnlocked;
        }

        public void StopObserving()
        {
            Events.OnDistrictBuilt -= OnDistrictBuilt;
            Events.OnUpgradeCardPicked -= OnUpgradePicked;
            Events.OnDistrictUnlocked -= OnDistrictUnlocked;
            
            unlockedDistricts.Clear();
        }

        private void OnDistrictUnlocked(TowerData towerData)
        {
            unlockedDistricts.Add(towerData.DistrictType);
        }

        private void OnUpgradePicked(UpgradeCardData.UpgradeCardInstance pickedUpgradeInstance)
        {
            for (int i = upgradeCardInstances.Count - 1; i >= 0; i--)
            {
                var upgradeInstance = upgradeCardInstances[i];
                if (upgradeInstance == pickedUpgradeInstance)
                {
                    if (upgradeInstance.WeightStrategy.HasFlag(WeightStrategy.RemoveOnPicked))
                    {
                        upgradeCardInstances.RemoveAtSwapBack(i);         
                        continue;
                    }

                    if (upgradeInstance.WeightStrategy.HasFlag(WeightStrategy.ChangeOnPicked))
                    {
                        upgradeInstance.Weight += upgradeInstance.WeightChangeOnPicked;
                    }
                }
                
                if (upgradeInstance.WeightStrategy.HasFlag(WeightStrategy.ChangeWithCardsPicked) 
                    && (pickedUpgradeInstance.UpgradeCardType & upgradeInstance.UpgradeCardType) > 0)
                {
                    upgradeInstance.Weight += upgradeInstance.WeightChangeOnCardsPicked;
                }
            }
        }

        private void OnDistrictBuilt(TowerData towerData)
        {
            for (int i = upgradeCardInstances.Count - 1; i >= 0; i--)
            {
                var upgradeInstance = upgradeCardInstances[i];
             
                if (upgradeInstance.WeightStrategy.HasFlag(WeightStrategy.ChangeWithDistrictsBuilt) 
                    && DoesMatch(towerData.DistrictType, upgradeInstance.UpgradeCardType))
                {
                    upgradeInstance.Weight += upgradeInstance.WeightChangeOnDistrictBuilt;
                }
            }
        }

        private static bool DoesMatch(DistrictType districtType, UpgradeCardType upgradeType)
        {
            return districtType switch
            {
                DistrictType.Lightning when (upgradeType & UpgradeCardType.Lightning) > 0 => true,
                DistrictType.TownHall when (upgradeType & UpgradeCardType.TownHall) > 0 => true,
                DistrictType.Archer when (upgradeType & UpgradeCardType.Archer) > 0 => true,
                DistrictType.Church when (upgradeType & UpgradeCardType.Church) > 0 => true,
                DistrictType.Flame when (upgradeType & UpgradeCardType.Flame) > 0 => true,
                DistrictType.Bomb when (upgradeType & UpgradeCardType.Bomb) > 0 => true,
                DistrictType.Mine when (upgradeType & UpgradeCardType.Mine) > 0 => true,
                _ => false
            };
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
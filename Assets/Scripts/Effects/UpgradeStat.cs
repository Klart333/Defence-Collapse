using Object = UnityEngine.Object;

using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using Buildings.District;
using Gameplay.Event;
using UnityEngine;
using Variables;
using Gameplay;
using System;
using UnityEngine.Serialization;

namespace Effects
{
    public interface IUpgradeStatEditor
    {
        public IUpgradeStat GetUpgradeStat(DistrictState districtState);
    }

    public interface IUpgradeStat
    {
        public string Name { get; }
        public string[] Descriptions { get; }
        public Sprite Icon { get; }

        public void IncreaseLevel();
        public float GetIncrease();
        public string GetFormat();
        public float GetCost();
    }

    #region Upgrade Stat
    
    [Serializable]
    public class UpgradeStatEditor : IUpgradeStatEditor
    {
        [Title("Visual")]
        [SerializeField]
        private StringReference statName;

        [SerializeField]
        private StringReference[] descriptions;

        [SerializeField]
        private Sprite statIcon;
     
        [Title("Stat")]
        [SerializeField]
        private bool useStatType;

        [FormerlySerializedAs("statTypeType")]
        [ShowIf(nameof(useStatType))]
        [SerializeField]
        private StatType statType;
        
        [Title("Upgrade Settings")]
        [SerializeField]
        private LevelData levelData;

        public IUpgradeStat GetUpgradeStat(DistrictState districtState)
        {
            Stat stat = useStatType ? districtState.Stats.StatDictionary[statType.Type] : new Stat(0);
            
            string[] descriptionsValues = new string[descriptions.Length];
            for (int i = 0; i < descriptions.Length; i++)
            {
                descriptionsValues[i] = descriptions[i].Value;
            }

            UpgradeStat upgradeStat = new UpgradeStat(districtState, stat, levelData, statName.Value, descriptionsValues, statIcon);
            return upgradeStat;
        }
    }

    public class UpgradeStat : IUpgradeStat
    {
        public event Action OnValueChanged;

        private Modifier increaseModifier;
        
        public DistrictState DistrictState { get; set; }
        public string[] Descriptions { get; }
        public int Level { get; set; } = 1;
        public LevelData LevelData { get; }
        public string Name { get; }
        public Sprite Icon { get; }
        public Stat Stat { get; }
        
        public float BaseValue => Stat.BaseValue;
        public float Value => Stat.Value;

        public UpgradeStat(DistrictState districtState, Stat stat, LevelData levelData, string name, string[] descriptions, Sprite icon)
        {
            DistrictState = districtState;
            Descriptions = descriptions;
            LevelData = levelData;
            Stat = stat;
            Name = name;
            Icon = icon;
            
            Stat.OnValueChanged += () => OnValueChanged?.Invoke();
        }
        
        public void AddModifier(Modifier mod)
        {
            Stat.AddModifier(mod);
        }

        public void RemoveModifier(Modifier mod)
        {
            Stat.RemoveModifier(mod);
        }

        public void RemoveAllModifiers()
        {
            Stat.RemoveAllModifiers();
        }

        public void IncreaseLevel()
        {
            if (increaseModifier == null)
            {
                increaseModifier = new Modifier
                {
                    Type = Modifier.ModifierType.Additive
                };

                Stat.AddModifier(increaseModifier);
            }
            
            increaseModifier.Value += GetIncrease();
            Stat.SetDirty(false);
            DistrictState.Level++;
            Level++;
        }

        public float GetCost()
        {
            return LevelData.GetCost(DistrictState.Level);
        }
        
        public float GetIncrease()
        {
            return LevelData.GetIncrease(Level);
        }

        public string GetFormat() => "N";
    }

    #endregion

    #region Town Hall Upgrade

    [Serializable]
    public class TownHallUpgradeStatEditor : IUpgradeStatEditor
    {
        [Title("Visual")]
        [SerializeField]
        private StringReference statName;

        [SerializeField]
        private StringReference[] descriptions;

        [SerializeField]
        private Sprite statIcon;
     
        [Title("Stat")]
        [SerializeField]
        private bool useStatType;

        [FormerlySerializedAs("statTypeType")]
        [ShowIf(nameof(useStatType))]
        [SerializeField]
        private StatType statType;
        
        [Title("Upgrade Settings")]
        [SerializeField]
        private LevelData levelData;

        public IUpgradeStat GetUpgradeStat(DistrictState districtState)
        {
            Stat stat = useStatType ? districtState.Stats.StatDictionary[statType.Type] : new Stat(0);
            
            string[] descriptionsValues = new string[descriptions.Length];
            for (int i = 0; i < descriptions.Length; i++)
            {
                descriptionsValues[i] = descriptions[i].Value;
            }

            TownHallUpgradeStat upgradeStat = new TownHallUpgradeStat(districtState, stat, levelData, statName.Value, descriptionsValues, statIcon);
            return upgradeStat;
        }
    }
    
    public class TownHallUpgradeStat : IUpgradeStat
    {
        public event Action OnValueChanged;

        private DistrictUnlockHandler districtUnlockHandler;
        private DistrictGenerator districtGenerator;
        private Modifier increaseModifier;
        
        public DistrictState DistrictState { get; }
        public string[] Descriptions { get; }
        public LevelData LevelData { get; }
        private int Level { get; set; } = 1;
        public string Name { get; }
        public Sprite Icon { get; }
        public Stat Stat { get; }
        
        public float BaseValue => Stat.BaseValue;
        public float Value => Stat.Value;

        public TownHallUpgradeStat(DistrictState districtState, Stat stat, LevelData levelData, string name, string[] descriptions, Sprite icon)
        {
            DistrictState = districtState;
            Descriptions = descriptions;
            LevelData = levelData;
            Stat = stat;
            Name = name;
            Icon = icon;
            
            Stat.OnValueChanged += () => OnValueChanged?.Invoke();

            districtUnlockHandler = Object.FindFirstObjectByType<DistrictUnlockHandler>();
            districtGenerator = Object.FindFirstObjectByType<DistrictGenerator>();
        }

        public void IncreaseLevel()
        {
            IncreaseDistrictHeight();

            if (increaseModifier == null)
            {
                increaseModifier = new Modifier
                {
                    Type = Modifier.ModifierType.Additive
                };

                Stat.AddModifier(increaseModifier);
            }
            
            increaseModifier.Value += GetIncrease();
            Stat.SetDirty(false);
            DistrictState.Level++;
            Level++;
            
            UIEvents.OnFocusChanged?.Invoke();
            districtUnlockHandler.DisplayUnlockableDistricts();

            PersistantGameStats.CurrentPersistantGameStats.TownHallLevel++;
        }

        private void IncreaseDistrictHeight()
        {
            districtGenerator.AddAction(DistrictState.DistrictData.DistrictHandler.IncreaseTownHallHeight);
        }

        public float GetCost()
        {
            return LevelData.GetCost(DistrictState.Level);
        }
        
        public float GetIncrease()
        {
            return LevelData.GetIncrease(Level);
        }
        
        public string GetFormat() => "N0";
    }
    
    #endregion
    
    #region Upgrade State
    
    [Serializable]
    public class UpgradeStateEditor : IUpgradeStatEditor
    {
        [Title("Visual")]
        [SerializeField]
        private StringReference statName;

        [SerializeField]
        private StringReference[] descriptions;

        [SerializeField]
        private Sprite statIcon;
     
        [Title("Upgrade State")]
        [SerializeField]
        private TowerData towerData;
        
        [Title("Upgrade Settings")]
        [SerializeField]
        private LevelData levelData;

        public IUpgradeStat GetUpgradeStat(DistrictState districtState)
        {
            string[] descriptionsValues = new string[descriptions.Length];
            for (int i = 0; i < descriptions.Length; i++)
            {
                descriptionsValues[i] = descriptions[i].Value;
            }

            UpgradeState upgradeStat = new UpgradeState(districtState, towerData, levelData, statName.Value, descriptionsValues, statIcon);
            return upgradeStat;
        }
    }

    public class UpgradeState : IUpgradeStat
    {
        public DistrictState DistrictState { get; }
        public TowerData UpgradeStateData { get; }
        public string[] Descriptions { get; }
        public int Level { get; set; } = 1;
        public LevelData LevelData { get; }
        public string Name { get; }
        public Sprite Icon { get; }
        
        public UpgradeState(DistrictState districtState, TowerData upgradeStateData, LevelData levelData, string name, string[] descriptions, Sprite icon)
        {
            UpgradeStateData = upgradeStateData;
            DistrictState = districtState;
            Descriptions = descriptions;
            LevelData = levelData;
            Name = name;
            Icon = icon;
        }

        public void IncreaseLevel()
        {
            DistrictState.DistrictData.ChangeState(UpgradeStateData);
        }

        public float GetCost()
        {
            return LevelData.GetCost(DistrictState.Level);
        }
        
        public float GetIncrease()
        {
            return LevelData.GetIncrease(Level);
        }

        public string GetFormat() => "N";
    }

    #endregion

}
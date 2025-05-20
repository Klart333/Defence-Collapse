using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using Gameplay;
using System;
using System.Threading.Tasks;
using Buildings.District;
using UnityEngine.InputSystem;
using WaveFunctionCollapse;
using Object = UnityEngine.Object;

public interface IUpgradeStat : IStat
{
    public string Name { get; }
    public string[] Descriptions { get; }
    public Sprite Icon { get; }
    public int Level { get; }

    public float GetCost();
    public float GetIncrease();
    public void IncreaseLevel();
}

[Serializable, InlineProperty]
public class UpgradeStat : IUpgradeStat
{
    [Title("Stat")]
    public Stat Stat = new Stat(1);
    
    [Title("Upgrade Settings")]
    public string Name { get; } = "Stat Name";
    public string[] Descriptions { get; }
    public Sprite Icon { get; }
    
    public LevelData LevelData;

    private Modifier increaseModifier;

    public int Level { get; private set; } = 1;
    public event Action OnValueChanged;
    public float Value => Stat.Value;
    public float BaseValue => Stat.BaseValue;

    public UpgradeStat()
    {
        Stat.OnValueChanged += () => OnValueChanged?.Invoke();
    }

    public UpgradeStat(Stat stat, LevelData levelData, string name, string[] descriptions, Sprite icon)
    {
        Stat = stat;
        LevelData = levelData;
        Name = name;
        Descriptions = descriptions;
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
        Level++;
    }

    public float GetCost()
    {
        return LevelData.GetCost(Level);
    }
    
    public float GetIncrease()
    {
        return LevelData.GetIncrease(Level);
    }
}

public class TownHallUpgradeStat : IUpgradeStat
{
    [Title("Stat")]
    public Stat Stat = new Stat(1);
    
    [Title("Upgrade Settings")]
    public string Name { get; } = "Stat Name";
    public string[] Descriptions { get; }
    public Sprite Icon { get; }
    
    public LevelData LevelData;

    private Modifier increaseModifier;

    public int Level { get; private set; } = 1;
    public event Action OnValueChanged;
    public float Value => Stat.Value;
    public float BaseValue => Stat.BaseValue;

    public TownHallUpgradeStat(Stat stat, LevelData levelData, string name, string[] descriptions, Sprite icon)
    {
        Stat = stat;
        LevelData = levelData;
        Name = name;
        Descriptions = descriptions;
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
        Level++;
        
        UIEvents.OnFocusChanged?.Invoke();
        Object.FindFirstObjectByType<DistrictUnlockHandler>().DisplayUnlockableDistricts();
    }

    private void IncreaseDistrictHeight()
    {
        Object.FindFirstObjectByType<DistrictGenerator>().AddAction(Object.FindFirstObjectByType<DistrictHandler>().IncreaseTownHallHeight);
    }

    public float GetCost()
    {
        return LevelData.GetCost(Level);
    }
    
    public float GetIncrease()
    {
        return LevelData.GetIncrease(Level);
    }
}
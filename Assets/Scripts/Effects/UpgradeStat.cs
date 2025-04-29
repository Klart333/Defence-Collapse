using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using Gameplay;
using System;
using System.Threading.Tasks;
using UnityEngine.InputSystem;

public interface IUpgradeStat : IStat
{
    public string Name { get; }
    public string[] Descriptions { get; }
    public Sprite Icon { get; }
    public int Level { get; }

    public float GetCost();
    public float GetIncrease();
    public UniTask<bool> IncreaseLevel();
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

    public UniTask<bool> IncreaseLevel()
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
        Stat.SetDirty();
        Stat.InvokeValueChanged();
        Level++;

        return new UniTask<bool>(true);
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

    public TownHallUpgradeStat()
    {
        Stat.OnValueChanged += () => OnValueChanged?.Invoke();
    }

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

    public async UniTask<bool> IncreaseLevel()
    {
        bool suceeded = await IncreaseLevelAsync();
        if (!suceeded)
        {
            return false;
        }

        if (increaseModifier == null)
        {
            increaseModifier = new Modifier
            {
                Type = Modifier.ModifierType.Additive
            };

            Stat.AddModifier(increaseModifier);
        }
        
        increaseModifier.Value += GetIncrease();
        Stat.SetDirty();
        Stat.InvokeValueChanged();
        Level++;
        return true;
    }

    private async Task<bool> IncreaseLevelAsync()
    {
        bool placed = false;
        bool canceled = false;
        Events.OnDistrictClicked?.Invoke(DistrictType.TownHall, 2 + Level);
        Events.OnDistrictBuilt += OnCapitolUpgradePlaced;

        UIEvents.OnFocusChanged += OnFocusChanged;
        InputManager.Instance.Cancel.performed += CancelOnperformed;

        while (!canceled && !placed)
        {
            await UniTask.Yield();
        }
        
        UIEvents.OnFocusChanged -= OnFocusChanged;
        Events.OnDistrictBuilt -= OnCapitolUpgradePlaced;
        InputManager.Instance.Cancel.performed -= CancelOnperformed;
        return !canceled;

        void OnCapitolUpgradePlaced(DistrictType type) => placed = true;

        void CancelOnperformed(InputAction.CallbackContext obj) => canceled = true;
        void OnFocusChanged() => placed = true;
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
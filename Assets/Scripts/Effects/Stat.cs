using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using System;
using Gameplay;

public interface IStat
{
    public event Action OnValueChanged;
    public float Value { get; }

    public void AddModifier(Modifier mod);
    public void RemoveModifier(Modifier mod);
    public void RemoveAllModifiers();
}

[Serializable, InlineProperty]
public class Stat : IStat
{
    public event Action OnValueChanged;

    public float BaseValue
    {
        get => baseValue;
        set
        {
            if (Mathf.Approximately(baseValue, value)) return;

            baseValue = value;
            OnValueChanged?.Invoke();
        }
    }

    [SerializeField]
    private float baseValue;

    private HashSet<Modifier> modifiers = new HashSet<Modifier>();

    private float value;
    private bool isDirty = true;

    private readonly Modifier.ModifierType[] ModifierTypes =
    {
        Modifier.ModifierType.Additive,
        Modifier.ModifierType.Multiplicative,
    };

    [ShowInInspector, ReadOnly]
    public float Value
    {
        get
        {
            if (!isDirty)
            {
                return value;
            }
            
            value = GetValue();
            isDirty = false;

            return value;
        }
    }

    private float GetValue()
    {
        float val = BaseValue;
        if (modifiers == null)
        {
            return val;
        }

        foreach (Modifier.ModifierType modifierType in ModifierTypes)
        {
            foreach (Modifier modifier in modifiers)
            {
                if (modifier.Type != modifierType)
                {
                    continue;
                }
                
                val = modifier.Type switch
                {
                    Modifier.ModifierType.Multiplicative => val * modifier.Value,
                    Modifier.ModifierType.Additive => val + modifier.Value,
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
        }

        return val;
    }

    public Stat(float baseValue)
    {
        BaseValue = baseValue;
    }

    public void AddModifier(Modifier mod)
    {
        modifiers.Add(mod);

        isDirty = true;
        OnValueChanged?.Invoke();
    }

    public void RemoveModifier(Modifier mod)
    {
        if (!modifiers.Remove(mod))
        {
            Debug.LogError("Could not find modifier to remove");
            return;
        }

        isDirty = true;
        OnValueChanged?.Invoke();
    }

    public void RemoveAllModifiers()
    {
        modifiers.Clear();

        isDirty = true;
        OnValueChanged?.Invoke();
    }

    public void SetDirty()
    {
        isDirty = true;
    }
    
    public static implicit operator float(Stat stat) => stat.Value;
}

[Serializable]
public class Modifier
{
    public ModifierType Type;

    public float Value;

    public enum ModifierType
    {
        Additive = 0,
        Multiplicative = 1,
    }
}

[Serializable, InlineProperty]
public class UpgradeStat : IStat
{
    [Title("Stat")]
    public Stat Stat = new Stat(1);
    
    [Title("Upgrade Settings")]
    public string Name = "Stat Name";

    public LevelData LevelData;

    private Modifier increaseModifier;
    
    public int Level { get; private set; }
    public event Action OnValueChanged;
    public float Value => Stat.Value;

    public UpgradeStat()
    {
        Stat.OnValueChanged += () => OnValueChanged?.Invoke();
    }

    public UpgradeStat(Stat stat, LevelData levelData, string name)
    {
        Stat = stat;
        LevelData = levelData;
        Name = name;
        
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
        Stat.SetDirty();
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
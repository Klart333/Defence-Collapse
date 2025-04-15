using System.Collections.Generic;
using Sirenix.OdinInspector;
using Buildings.District;
using UnityEngine;
using Effects;
using System;

public class DistrictUpgradeManager : Singleton<DistrictUpgradeManager>
{
    [Title("State Data")]
    [SerializeField]
    private TowerData archerData;

    [SerializeField]
    private TowerData bombData;

    [SerializeField]
    private TowerData mineData;
    
    [Title("UI")]
    [SerializeField]
    private GameObject canvas;

    [Title("UI", "Upgrade")]
    [SerializeField]
    private UIDistrictUpgrade districtUpgrade;

    private readonly List<EffectModifier> modifierEffectsToSpawn = new List<EffectModifier>();

    public List<EffectModifier> ModifierEffects => modifierEffectsToSpawn;
    public TowerData ArcherData => archerData;
    public TowerData BombData => bombData;
    public TowerData MineData => mineData; 

    protected override void Awake()
    {
        base.Awake();

        InputManager.Instance.Cancel.performed += Cancel_performed;
    }

    private void Cancel_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        if (canvas.activeSelf)
        {
            Close();
        }
    }

    public void OpenUpgradeMenu(DistrictData district)
    {
        canvas.SetActive(true);
        districtUpgrade.ShowUpgrades(district);
    }

    public void Close()
    {
        canvas.SetActive(false);
        districtUpgrade.Close();
    }

    #region Modifier Effects

    public void AddModifierEffect(EffectModifier effect)
    {
        modifierEffectsToSpawn.Add(effect);
    }
        
    #endregion
}

[Serializable]
public class EffectModifier
{
    [Title("Info")]
    [PreviewField]
    public Sprite Icon;

    public string Description;
    public string Title;

    public EffectType EffectType;

    [Title("Effects")]
    public List<IEffect> Effects;

    public EffectModifier(Sprite icon, string description, string title, EffectType effectType, List<IEffect> effects)
    {
        Icon = icon;
        Description = description;
        Title = title;
        EffectType = effectType;
        Effects = effects;
    }

    public EffectModifier(EffectModifier copy)
    {
        Icon = copy.Icon;
        Description = copy.Description;
        Title = copy.Title;
        EffectType = copy.EffectType;
        Effects = new List<IEffect>();

        foreach (IEffect effect in copy.Effects)
        {
            if (effect is IEffectHolder holder)
            {
                Effects.Add(holder.Clone());
            }
            else
            {
                Effects.Add(effect);
            }
        }
    }
}
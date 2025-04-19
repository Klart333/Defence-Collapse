using System.Collections.Generic;
using Sirenix.OdinInspector;
using Buildings.District;
using UnityEngine;
using Effects;
using System;
using Cysharp.Threading.Tasks;

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
    
    private bool inOtherUI = false;

    public List<EffectModifier> ModifierEffects => modifierEffectsToSpawn;
    public TowerData ArcherData => archerData;
    public TowerData BombData => bombData;
    public TowerData MineData => mineData;

    private async void OnEnable()
    {
        UIEvents.OnFocusChanged += Close;    
        Events.OnBuildingClicked += OpenOtherUI;
        Events.OnDistrictClicked += OpenOtherUI;
        
        await UniTask.WaitUntil(() => InputManager.Instance != null);
        InputManager.Instance.Cancel.performed += Cancel_performed;
    }

    private void OnDisable()
    {
        InputManager.Instance.Cancel.performed -= Cancel_performed;
        Events.OnBuildingClicked -= OpenOtherUI;
        Events.OnDistrictClicked -= OpenOtherUI;
        UIEvents.OnFocusChanged -= Close;
    }
    
    private void OpenOtherUI(DistrictType arg0)
    {
        inOtherUI = true;
    }

    private void OpenOtherUI(BuildingType arg0)
    {
        inOtherUI = true;
    }

    private void Cancel_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        inOtherUI = false;
        Close();
    }

    public void OpenUpgradeMenu(DistrictData district)
    {
        if (inOtherUI)
        {
            return;
        }
        
        UIEvents.OnFocusChanged?.Invoke();
        
        canvas.SetActive(true);
        districtUpgrade.ShowUpgrades(district);
    }

    public void Close()
    {
        if (!canvas.activeSelf) return;

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
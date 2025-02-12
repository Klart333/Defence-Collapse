using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Effects;
using System;
using WaveFunctionCollapse;

public class BuildingUpgradeManager : Singleton<BuildingUpgradeManager>
{
    [Title("State Data")]
    [SerializeField]
    private TowerData archerData;

    [SerializeField]
    private TowerData bombData;

    [SerializeField]
    private NormalHouseData normalData;

    [Title("Mesh Information")]
    [SerializeField]
    private TowerMeshData towerMeshData;

    [SerializeField]
    private PrototypeInfoData buildingPrototypes;

    [Title("UI")]
    [SerializeField]
    private GameObject canvas;

    [Title("UI", "Advancement")]
    [SerializeField]
    private GameObject advancementPanel;

    [Title("UI", "Upgrade")]
    [SerializeField]
    private UIBuildingUpgrade buildingUpgrade;

    private List<EffectModifier> modifierEffectsToSpawn = new List<EffectModifier>();

    private BuildingHandler buildingHandler;
    private Building currentBuilding;

    public List<EffectModifier> ModifierEffects => modifierEffectsToSpawn;
    public TowerData BombData => bombData;
    public TowerData ArcherData => archerData;
    public NormalHouseData NormalData => normalData;

    protected override void Awake()
    {
        base.Awake();

        buildingHandler = FindAnyObjectByType<BuildingHandler>();
        InputManager.Instance.Cancel.performed += Cancel_performed;
    }

    private void Cancel_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        if (canvas.activeSelf)
        {
            Close();
        }
    }

    public void OpenAdvancementMenu(Building building)
    {
        currentBuilding = building;

        canvas.SetActive(true);
        advancementPanel.SetActive(true);
    }

    public void OpenUpgradeMenu(Building building)
    {
        currentBuilding = building;

        canvas.SetActive(true);
        //buildingUpgrade.ShowUpgrades(buildingHandler[building]);
    }

    public void Close()
    {
        currentBuilding.BuildingUI.OnMenuClosed();
        currentBuilding = null;

        canvas.SetActive(false);
        advancementPanel.SetActive(false);
        buildingUpgrade.Close();
    }

    public void SelectAdvancementOption(TowerType towerType)
    {
        Debug.LogError("DEPRECATED");
        (PrototypeData, BuildingCellInformation)? data = towerMeshData.GetInfo(towerType, buildingPrototypes.Prototypes);
        if (data.HasValue)
        {
            //buildingHandler[currentBuilding].AdvanceState(data.Value.Item2, data.Value.Item1);
        }
        else
        {
            Debug.LogError("Wtf man");
        }

        Close();
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
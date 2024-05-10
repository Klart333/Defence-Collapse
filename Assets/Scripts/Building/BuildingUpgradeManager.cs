using Effects;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

public class BuildingUpgradeManager : Singleton<BuildingUpgradeManager>
{
    [Title("State Data")]
    [SerializeField]
    private ArcherData archerData;

    [SerializeField]
    private NormalHouseData normalData;

    [Title("Mesh Information")]
    [SerializeField]
    private TowerMeshData towerMeshData;

    [SerializeField]
    private PrototypeInfoCreator buildingPrototypes;

    [Title("UI")]
    [SerializeField]
    private GameObject canvas;

    [Title("UI", "Advancement")]
    [SerializeField]
    private GameObject advancementPanel;

    [Title("UI", "Upgrade")]
    [SerializeField]
    private UIBuildingUpgrade buildingUpgrade;

    private List<EffectModifier> ModifierEffects = new List<EffectModifier>();

    private BuildingHandler buildingHandler;
    private Building currentBuilding;

    public ArcherData ArcherData => archerData;
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
        buildingUpgrade.ShowUpgrades(buildingHandler[building]);
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
        var data = towerMeshData.GetInfo(towerType, buildingPrototypes.Prototypes);
        if (data.HasValue)
        {
            buildingHandler[currentBuilding].AdvanceState(data.Value.Item2, data.Value.Item1);
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
        ModifierEffects.Add(effect);
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

    public EffectType EffectType;

    [Title("Effects")]
    public List<IEffect> Effects;
}
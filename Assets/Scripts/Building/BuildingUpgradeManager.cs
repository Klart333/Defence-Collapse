using Sirenix.OdinInspector;
using System;
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

    [Title("UI", "Advancement")]
    [SerializeField]
    private GameObject advancementPanel;

    [Title("UI", "Upgrade")]
    [SerializeField]
    private UIBuildingUpgrade buildingUpgrade;

    private BuildingHandler buildingHandler;
    private Building currentBuilding;

    public ArcherData ArcherData => archerData;
    public NormalHouseData NormalData => normalData;

    protected override void Awake()
    {
        base.Awake();

        buildingHandler = FindAnyObjectByType<BuildingHandler>();
    }

    public void OpenAdvancementMenu(Building building)
    {
        currentBuilding = building;

        advancementPanel.SetActive(true);
    }

    public void OpenUpgradeMenu(Building building)
    {
        currentBuilding = building;

        buildingUpgrade.ShowUpgrades(buildingHandler[building].CellInformation.TowerType);
    }

    public void Close()
    {
        currentBuilding.BuildingUI.OnMenuClosed();
        currentBuilding = null;

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
}

using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

[System.Serializable]   
public class BuildingData
{
    private BuildingHandler handler;
    private BuildingState state;

    public BuildingCellInformation CellInformation {  get; private set; } 
    public UpgradeData UpgradeData { get; private set; }
    public PrototypeData Prototype { get; set; }
    public Vector3Int Index { get; set; }
    public Health Health { get; set; }

    public BuildingState State => state;

    public BuildingData(BuildingHandler buildingHandler)
    {
        handler = buildingHandler;
        UpgradeData = new UpgradeData(1, 1, 1);

        Events.OnWaveStarted += OnWaveStarted;
    }

    public void SetState(BuildingCellInformation cellInfo, Vector3Int index, PrototypeData prot)
    {
        switch (cellInfo.TowerType)
        {
            case TowerType.None:
                state = new NormalState(this, BuildingUpgradeManager.Instance.NormalData);

                break;
            case TowerType.Archer:
                state = new ArcherState(this, BuildingUpgradeManager.Instance.ArcherData);

                break;

            case TowerType.Bomb:
                state = new BombState(this, BuildingUpgradeManager.Instance.BombData);

                break;
            default:
                break;
        }

        Health = new Health(state);

        Prototype = prot;
        Index = index;
        CellInformation = cellInfo;

        State.OnStateEntered();

        Health.OnDeath += OnBuildingDeath;
    }

    public void UpdateState(BuildingCellInformation cellInfo, PrototypeData prot)
    {
        switch (cellInfo.TowerType)
        {
            case TowerType.None:
                state = new NormalState(this, BuildingUpgradeManager.Instance.NormalData);

                break;
            case TowerType.Archer:
                state = new ArcherState(this, BuildingUpgradeManager.Instance.ArcherData);

                break;
            case TowerType.Bomb:
                state = new BombState(this, BuildingUpgradeManager.Instance.BombData);

                break;
            default:
                break;
        }
        Health.UpdateAttacker(state);

        Prototype = prot;
        CellInformation = cellInfo;

        State.OnStateEntered();
    }

    public void AdvanceState(BuildingCellInformation cellInfo, PrototypeData prot)
    {
        UpdateState(cellInfo, prot);
        Building building = handler.GetBuilding(Index);

        building.Setup(prot, building.MeshRenderer.transform.localScale);
        building.DisplayLevelUp();
    }

    public void OnBuildingChanged(BuildingCellInformation cellInfo, Building building)
    {
        if (!CellInformation.Equals(cellInfo))
        {
            UpdateState(cellInfo, building.Prototype);
        }

        building.SetData(this);
    }

    public void OnBuildingDeath()
    {
        handler.BuildingDestroyed(Index);
    }

    private void OnWaveStarted()
    {
        State.OnWaveStart(CellInformation.HouseCount);
    }

    public void LevelUp()
    {
        handler.DislpayLevelUp(Index);
    }

    public void Update(Building building)
    {
        if (!Health.Alive)
        {
            return;
        }

        State.Update(building);
    }
}

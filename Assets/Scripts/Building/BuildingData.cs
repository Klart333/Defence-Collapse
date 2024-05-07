using System;
using UnityEngine;

[System.Serializable]   
public class BuildingData
{
    private BuildingHandler handler;
    private BuildingState state;

    public BuildingCellInformation CellInformation {  get; private set; } 
    public PrototypeData Prototype { get; set; }
    public int BuildingLevel { get; set; }
    public Vector3Int Index { get; set; }
    public Health Health { get; set; }

    public BuildingState State => state;

    public BuildingData(BuildingHandler buildingHandler)
    {
        handler = buildingHandler;

        Events.OnWaveStarted += OnWaveStarted;
    }

    public void SetState(BuildingCellInformation cellInfo, Vector3Int index, PrototypeData prot)
    {
        switch (cellInfo.TowerType)
        {
            case TowerType.None:
                state = new NormalState(handler.NormalData);
                Health = new Health(handler.NormalData.MaxHealth);

                break;
            case TowerType.Archer:
                state = new ArcherState(handler.ArcherData, this);
                Health = new Health(handler.ArcherData.Stats.MaxHealth.Value);

                break;
            default:
                break;
        }

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
                state = new NormalState(handler.NormalData);
                Health.SetMaxHealth(handler.NormalData.MaxHealth);

                break;
            case TowerType.Archer:
                state = new ArcherState(handler.ArcherData, this);
                Health.SetMaxHealth(handler.ArcherData.Stats.MaxHealth.Value);

                break;
            default:
                break;
        }

        Prototype = prot;
        CellInformation = cellInfo;

        State.OnStateEntered();
    }

    public void OnBuildingChanged(Building building) // ??
    {
        building.SetData(this);
    }

    public void OnBuildingDeath()
    {
        handler.BuildingDestroyed(Index);
    }

    private void OnWaveStarted()
    {
        
    }

    private void LevelUp()
    {
        BuildingLevel += 1;
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

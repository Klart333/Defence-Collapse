using UnityEngine;

public class BuildingData
{
    private BuildingHandler handler;
    private BuildingState state;

    private int houseCount = 0;

    public Health Health { get; set; }
    public Vector3Int Index { get; set; }
    public int BuildingLevel { get; set; }

    public BuildingState State => state;

    public BuildingData(BuildingHandler buildingHandler)
    {
        handler = buildingHandler;
    }

    public void SetState(BuildingCellInformation cellInfo)
    {
        switch (cellInfo.TowerType)
        {
            case TowerType.None:
                state = new NormalState(handler.NormalData);
                Health = new Health(handler.NormalData.MaxHealth);

                break;
            case TowerType.Archer:
                state = new ArcherState(handler.ArcherData);
                Health = new Health(handler.ArcherData.MaxHealth);

                break;
            default:
                break;
        }

        houseCount = cellInfo.HouseCount;

        state.OnStateEntered();

        Health.OnDeath += OnBuildingDeath;
        Events.OnWaveStarted += OnWaveStarted;
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

}

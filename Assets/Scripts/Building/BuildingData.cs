using UnityEngine;
using WaveFunctionCollapse;

[System.Serializable]   
public class BuildingData : IHealth // CHANGE TO WALLDATA, IT TAKES DAMAGE, NOT A DISTRICT
{
    private BuildingHandler handler;

    public BuildingCellInformation CellInformation {  get; private set; } 
    public PrototypeData Prototype { get; set; }
    public Vector3Int Index { get; set; }

    public HealthComponent Health { get; set; }
    public Vector3 OriginPosition => Health.OriginPosition;

    public BuildingData(BuildingHandler buildingHandler)
    {
        handler = buildingHandler;
        //Health = new HealthComponent(null, null);
    }

    public void OnBuildingChanged(BuildingCellInformation cellInfo, Building building)
    {
        if (!CellInformation.Equals(cellInfo))
        {
            //UpdateState(cellInfo, building.Prototype);
        }

        building.SetData(this);
    }

    public void OnBuildingDeath()
    {
        handler.BuildingDestroyed(Index);
    }

    public void LevelUp()
    {
        handler.DislpayLevelUp(Index);
    }

    public void Update(Building building)
    {
        return;
        if (!Health.Alive)
        {
            return;
        }
    }
    
    public void TakeDamage(DamageInstance damage, out DamageInstance damageDone)
    {
        Health.TakeDamage(damage, out damageDone);
    }
}

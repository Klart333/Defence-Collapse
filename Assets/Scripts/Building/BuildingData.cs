using Unity.Mathematics;
using UnityEngine;
using WaveFunctionCollapse;

[System.Serializable]   
public class BuildingData : IHealth // CHANGE TO WALLDATA, IT TAKES DAMAGE, NOT A DISTRICT
{
    private BuildingHandler handler;

    public BuildingCellInformation CellInformation {  get; private set; } 
    public PrototypeData Prototype { get; set; }
    public int2 Index { get; set; }

    public HealthComponent Health { get; set; }

    public BuildingData(BuildingHandler buildingHandler, Stats stats, int2 index)
    {
        handler = buildingHandler;
        Health = new HealthComponent(stats);
        Index = index;
    }

    public void OnBuildingChanged(BuildingCellInformation cellInfo, Building building)
    {
        if (!CellInformation.Equals(cellInfo))
        {
            //UpdateState(cellInfo, building.Prototype);
        }

        building.SetData(this);
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
    }
    
    public void TakeDamage(DamageInstance damage, out DamageInstance damageDone)
    {
        Health.TakeDamage(damage, out damageDone);

        if (!Health.Alive)
        {
            OnBuildingDeath();
        }
    }
    
    public void TakeDamage(float damage)
    {
        Health.TakeDamage(damage);

        if (!Health.Alive)
        {
            OnBuildingDeath();
        }
    }
    
    public void OnBuildingDeath()
    {
        handler.BuildingDestroyed(Index);
    }
}

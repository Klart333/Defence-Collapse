using WaveFunctionCollapse;

[System.Serializable]   
public class WallState : IHealth
{
    private BuildingHandler handler;

    public BuildingCellInformation CellInformation {  get; private set; } 
    public PrototypeData Prototype { get; set; }
    public ChunkIndex Index { get; set; }

    public HealthComponent Health { get; set; }

    public WallState(BuildingHandler buildingHandler, Stats stats, ChunkIndex index)
    {
        handler = buildingHandler;
        Health = new HealthComponent(stats);
        Index = index;
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

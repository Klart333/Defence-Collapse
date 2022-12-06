public class BuildingPurchaser
{
    private MoneyManager MoneyManager;

    public BuildingPurchaser(MoneyManager money)
    {
        MoneyManager = money;
    }

    public bool CanPurchaseBuilding(Building building)
    {
        return building.Cost <= MoneyManager.MoneyAmount;
    }
}

public interface ILoot
{
    string Description { get; }

    void Perform();
}

public class GoldLoot : ILoot
{
    public string Description => "Some gold";

    public void Perform()
    {
        MoneyManager.Instance.AddMoney(10);
    }
}
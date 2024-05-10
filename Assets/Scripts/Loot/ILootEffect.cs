using Effects;
using Sirenix.OdinInspector;
using UnityEngine;

public interface ILootEffect
{
    string GetDescription(int grade);

    void Perform(int grade);
}

public class GoldLoot : ILootEffect
{
    [Title("Effect")]
    public int Gold = 10;

    [Title("Grade")]
    public float GradeMultiplier = 1;

    public void Perform(int grade)
    {
        MoneyManager.Instance.AddMoney(Gold + Mathf.RoundToInt(Gold * grade * GradeMultiplier));
    }

    public string GetDescription(int grade)
    {
        return string.Format("Found {0} gold pieces", Gold + Mathf.RoundToInt(Gold * grade * GradeMultiplier));
    }
}


public class EffectLoot : ILootEffect
{
    [Title("Effect")]
    public EffectModifier Effect;

    public void Perform(int grade)
    {
        BuildingUpgradeManager.Instance.AddModifierEffect(Effect);
    }

    public string GetDescription(int grade)
    {
        return Effect.Description;
    }
}
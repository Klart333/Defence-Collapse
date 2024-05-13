using Sirenix.OdinInspector;
using UnityEngine;

public interface ILootEffect
{
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
}


public class EffectLoot : ILootEffect
{
    [Title("Effect")]
    public EffectModifier Effect;

    public void Perform(int grade)
    {
        EffectModifier effect = new EffectModifier(Effect);
        BuildingUpgradeManager.Instance.AddModifierEffect(effect);
        LootManager.Instance.DisplayEffectGained(effect);
    }
}
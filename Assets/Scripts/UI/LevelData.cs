using Sirenix.OdinInspector;
using UnityEngine;

[InlineEditor, CreateAssetMenu(fileName = "New Level Data", menuName = "Building/Level Data")]
public class LevelData : ScriptableObject
{
    [Title("Increase")]
    public float AttackSpeedIncrease = 0.2f;
    public float DamageIncrease = 0.2f;
    public float RangeIncrease = 0.6f;

    [Title("Cost")]
    public float AttackSpeedCost = 1;
    public float DamageCost = 1;
    public float RangeCost = 1f;

    public float GetIncrease(LevelStat stat, float current)
    {
        switch (stat)
        {
            case LevelStat.AttackSpeed:
                return current * AttackSpeedIncrease;
            case LevelStat.Damage:
                return current * DamageIncrease;
            case LevelStat.Range:
                return current * RangeIncrease;
        }

        return 0;
    }

    public float GetCost(LevelStat stat, int level)
    {
        switch (stat)
        {
            case LevelStat.AttackSpeed:
                return level * 2 + Mathf.Pow(AttackSpeedCost, level);
            case LevelStat.Damage:
                return level * 2 + Mathf.Pow(DamageCost, level);
            case LevelStat.Range:
                return level * 2 + Mathf.Pow(RangeCost, level);
        }

        return 0;
    }
}

public enum LevelStat
{
    AttackSpeed,
    Damage,
    Range
}
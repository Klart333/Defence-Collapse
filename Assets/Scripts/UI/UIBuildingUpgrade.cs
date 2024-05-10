using Sirenix.OdinInspector;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIBuildingUpgrade : MonoBehaviour
{
    [Title("Level Data")]
    [SerializeField]
    private LevelData levelData;

    [Title("Panel")]
    [SerializeField]
    private GameObject parentPanel;

    [Title("Displays")]
    [SerializeField]
    private UIUpgradeDisplay attackSpeedDisplay;

    [SerializeField]
    private UIUpgradeDisplay damageDisplay;

    [SerializeField]
    private UIUpgradeDisplay rangeDisplay;

    [Title("Misc")]
    [SerializeField]
    private int barCount = 15;
    
    private BuildingData currentData;

    public void ShowUpgrades(BuildingData buildingData)
    {
        currentData = buildingData;

        parentPanel.SetActive(true);

        DisplayStats();
    }

    private void DisplayStats()
    {
        attackSpeedDisplay.DisplayStat((float)currentData.UpgradeData.Attackspeed / barCount, levelData.GetCost(LevelStat.AttackSpeed, currentData.UpgradeData.Attackspeed));
        damageDisplay     .DisplayStat((float)currentData.UpgradeData.Damage      / barCount, levelData.GetCost(LevelStat.Damage     , currentData.UpgradeData.Damage     ));
        rangeDisplay      .DisplayStat((float)currentData.UpgradeData.Range       / barCount, levelData.GetCost(LevelStat.Range      , currentData.UpgradeData.Range      ));
    }

    public void Close()
    {
        parentPanel.SetActive(false);
    }

    public void UpgradeStat(LevelStat stat)
    {
        MoneyManager.Instance.RemoveMoney(levelData.GetCost(stat, currentData.UpgradeData.GetStatLevel(stat)));

        currentData.UpgradeData.IncreaseStat(stat, 1);

        switch (stat)
        {
            case LevelStat.AttackSpeed:
                currentData.State.Stats.AttackSpeed.BaseValue += levelData.GetIncrease(stat, currentData.UpgradeData.Attackspeed);
                break;
            case LevelStat.Damage:
                currentData.State.Stats.DamageMultiplier.BaseValue += levelData.GetIncrease(stat, currentData.UpgradeData.Damage);
                break;
            case LevelStat.Range:
                currentData.State.Range += levelData.GetIncrease(stat, currentData.UpgradeData.Range);
                break;
            default:
                break;
        }
        currentData.LevelUp();

        DisplayStats();
    }

    public bool CanPurchase(LevelStat stat)
    {
        return MoneyManager.Instance.Money >= levelData.GetCost(stat, currentData.UpgradeData.GetStatLevel(stat));
    }
}

public class UpgradeData
{
    public int Attackspeed;
    public int Damage;
    public int Range;

    public UpgradeData(int speed, int damage, int range)
    {
        Attackspeed = speed;
        Damage = damage;
        Range = range;
    }

    public int GetStatLevel(LevelStat levelStat) 
    {
        switch (levelStat)
        {
            case LevelStat.AttackSpeed:
                return Attackspeed;
            case LevelStat.Damage:
                return Damage;
            case LevelStat.Range:
                return Range;
        }

        return -1;
    }

    public void IncreaseStat(LevelStat stat, int increase)
    {
        switch (stat)
        {
            case LevelStat.AttackSpeed:
                Attackspeed += increase;
                break;
            case LevelStat.Damage:
                Damage += increase;
                break;
            case LevelStat.Range:
                Range += increase;
                break;
            default:
                break;
        }
    }
}

using Sirenix.OdinInspector;
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

    [Title("Fills")]
    [SerializeField]
    private Image attackSpeedFill;

    [SerializeField]
    private Image damageFill;
    
    [SerializeField]
    private Image rangeFill;

    private BuildingData currentData;

    public void ShowUpgrades(BuildingData buildingData)
    {
        currentData = buildingData;

        parentPanel.SetActive(true);

        DisplayStats();
    }

    private void DisplayStats()
    {
        attackSpeedFill.fillAmount = (float)currentData.UpgradeData.Attackspeed / 10.0f;
        damageFill.fillAmount = (float)currentData.UpgradeData.Damage / 10.0f;
        rangeFill.fillAmount = (float)currentData.UpgradeData.Range / 10.0f;
    }

    public void Close()
    {
        parentPanel.SetActive(false);
    }

    public void UpgradeStat(LevelStat stat)
    {
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

    public void UpgradeAttackspeed()
    {
        UpgradeStat(LevelStat.AttackSpeed);
    }

    public void UpgradeDamage()
    {
        UpgradeStat(LevelStat.Damage);
    }

    public void UpgradeRange()
    {
        UpgradeStat(LevelStat.Range);
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

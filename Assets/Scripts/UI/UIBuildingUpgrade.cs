using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
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

    [Title("UI")]
    [SerializeField]
    private TextMeshProUGUI upgradeTitleText;

    [SerializeField]
    private TextMeshProUGUI costText;

    [SerializeField]
    private TextMeshProUGUI[] descriptions;

    [SerializeField]
    private Image[] points;

    [Title("Upgrade Displays")]
    [SerializeField]
    private List<UIUpgradeDisplay> displays;

    private BuildingData currentData;
    private Canvas canvas;

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
    }

    private void Update()
    {
        PositionRectTransform.PositionOnOverlayCanvas(canvas, transform as RectTransform, BuildingManager.Instance.GetPos(currentData.Index), new Vector2(0.25f, 0.5f));
    }

    public void ShowUpgrades(BuildingData buildingData)
    {
        currentData = buildingData;

        parentPanel.SetActive(true);

        DisplayStats();
    }

    public void DisplayUpgrade(string upgradeName, List<string> descriptionStrings, LevelStat stat)
    {
        upgradeTitleText.text = upgradeName;

        for (int i = 0; i < descriptions.Length; i++)
        {
            if (i >= descriptionStrings.Count)
            {
                descriptions[i].text = "";
                points[i].gameObject.SetActive(false);
            }
            else
            {
                descriptions[i].text = descriptionStrings[i];
                points[i].gameObject.SetActive(true);
            }
        }

        costText.text = levelData.GetCost(stat, currentData.UpgradeData.GetStatLevel(stat)).ToString();
    }

    private void DisplayStats()
    {
        for (int i = 0; i < displays.Count; i++)
        {
            displays[i].DisplayStat(currentData);
        }
    }

    public void Close()
    {
        parentPanel.SetActive(false);

        for (int i = 0; i < displays.Count; i++)
        {
            displays[i].Close();
        }
    }

    public void UpgradeStat(LevelStat stat)
    {
        MoneyManager.Instance.RemoveMoney(levelData.GetCost(stat, currentData.UpgradeData.GetStatLevel(stat)));

        currentData.UpgradeData.IncreaseStat(stat, 1);

        switch (stat)
        {
            case LevelStat.AttackSpeed:
                currentData.State.Stats.AttackSpeed.BaseValue += GetIncrease(stat, currentData.UpgradeData.Attackspeed);
                break;
            case LevelStat.Damage:
                currentData.State.Stats.DamageMultiplier.BaseValue += GetIncrease(stat, currentData.UpgradeData.Damage);
                break;
            case LevelStat.Range:
                currentData.State.Range += GetIncrease(stat, currentData.UpgradeData.Range);
                break;
            default:
                break;
        }
        currentData.LevelUp();

        DisplayStats();
    }

    public float GetIncrease(LevelStat stat, int level)
    {
        return levelData.GetIncrease(stat, level);
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

public static class PositionRectTransform // Chat gippity
{
    // Function to position RectTransform on Overlay Canvas to align with world position
    public static void PositionOnOverlayCanvas(Canvas canvas, RectTransform rectTransform, Vector3 worldPosition, Vector2 pivot)
    {
        // Convert world position to screen space
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main, worldPosition);

        // Convert screen space to canvas space
        Vector2 canvasPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, screenPoint, canvas.worldCamera, out canvasPos);

        // Calculate offset based on pivot
        Vector2 pivotOffset = new Vector2(
            rectTransform.rect.width * (pivot.x - 0.5f),
            rectTransform.rect.height * (pivot.y - 0.5f)
        );

        // Set anchored position
        rectTransform.anchoredPosition = canvasPos + pivotOffset;
    }
}
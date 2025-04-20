using System;
using System.Collections.Generic;
using Buildings.District;
using Sirenix.OdinInspector;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine;
using TMPro;

public class UIDistrictUpgrade : MonoBehaviour
{
    [Title("Level Data")]
    [SerializeField]
    private LevelData levelData;

    [Title("Panel")]
    [SerializeField]
    private GameObject parentPanel;

    [Title("Upgrade")]
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

    [Title("Sections")]
    [SerializeField]
    private GameObject upgradeSection;

    [SerializeField]
    private GameObject modifySection;

    [SerializeField]
    private RectTransform line;

    [Title("Effect Panels")]
    [SerializeField]
    private UIEffectsHandler ownedEffectsPanel;

    [SerializeField]
    private UIEffectsHandler towerEffectsPanel;

    private DistrictData currentData;
    private Canvas canvas;
    private Camera cam;

    public LevelData LevelData => levelData;

    private void OnEnable()
    {
        canvas = GetComponentInParent<Canvas>();

        towerEffectsPanel.OnEffectAdded += AddEffectToTower;
        towerEffectsPanel.OnEffectRemoved += RemoveEffectFromTower;
    }

    private void OnDisable()
    {
        towerEffectsPanel.OnEffectAdded -= AddEffectToTower;
        towerEffectsPanel.OnEffectRemoved -= RemoveEffectFromTower;
    }

    private void Update()
    {
        PositionRectTransform.PositionOnOverlayCanvas(canvas, cam, transform as RectTransform, currentData.Position, new Vector2(0.25f, 0.5f));
    }

    #region UI

    public void ShowSection(bool isUpgrade)
    {
        upgradeSection.SetActive(isUpgrade);
        modifySection.SetActive(!isUpgrade);

        //line.DORewind();
        line.DOAnchorPosX(isUpgrade ? -100 : 100, 0.2f).SetEase(Ease.OutCirc);

        (ownedEffectsPanel.transform.parent as RectTransform).DOAnchorPosX(isUpgrade ? -0 : -420, 0.5f).SetEase(Ease.OutCirc);
    }

    public void ShowUpgrades(DistrictData districtData)
    {
        currentData = districtData;

        parentPanel.SetActive(true);
        ownedEffectsPanel.SpawnEffects();
        (ownedEffectsPanel.transform.parent as RectTransform).anchoredPosition = Vector2.zero;

        ShowSection(true);
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
    #endregion

    #region Functionality

    public void UpgradeStat(LevelStat stat)
    {
        MoneyManager.Instance.RemoveMoney(levelData.GetCost(stat, currentData.UpgradeData.GetStatLevel(stat)));

        currentData.UpgradeData.IncreaseStat(stat, 1);

        switch (stat)
        {
            case LevelStat.AttackSpeed:
                currentData.State.Stats.AttackSpeed.BaseValue += levelData.GetIncrease(stat, currentData.State.Stats.AttackSpeed.Value);
                break;
            case LevelStat.Damage:
                currentData.State.Stats.DamageMultiplier.BaseValue += levelData.GetIncrease(stat, currentData.State.Stats.DamageMultiplier.Value);
                break;
            case LevelStat.Range:
                currentData.State.Range += levelData.GetIncrease(stat, currentData.State.Range);
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


    private void AddEffectToTower(EffectModifier effectModifier)
    {
        currentData.State.Attack.AddEffect(effectModifier.Effects, effectModifier.EffectType);
    }

    private void RemoveEffectFromTower(EffectModifier modifier)
    {
        currentData.State.Attack.RemoveEffect(modifier.Effects, modifier.EffectType);
    }

    #endregion
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
    public static void PositionOnOverlayCanvas(Canvas canvas, Camera cam, RectTransform rectTransform, Vector3 worldPosition, Vector2 pivot)
    {
        // Convert world position to screen space
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldPosition);

        // Convert screen space to canvas space
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, screenPoint, canvas.worldCamera, out Vector2 canvasPos);

        // Calculate offset based on pivot
        Vector2 pivotOffset = new Vector2(
            rectTransform.rect.width * (pivot.x - 0.5f),
            rectTransform.rect.height * (pivot.y - 0.5f)
        );

        // Set anchored position
        rectTransform.anchoredPosition = canvasPos + pivotOffset;
    }
}
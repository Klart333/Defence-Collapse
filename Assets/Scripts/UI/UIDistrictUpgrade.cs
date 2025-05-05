using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Buildings.District;
using UnityEngine.UI;
using Gameplay.Money;
using UnityEngine;
using DG.Tweening;
using System;
using Loot;
using TMPro;

public class UIDistrictUpgrade : MonoBehaviour
{
    [Title("Panel")]
    [SerializeField]
    private GameObject parentPanel;
    
    [SerializeField]
    private Vector2 pivot = new Vector2(0.5f, 0.5f);

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
    private TextMeshProUGUI panelTitleText;
    
    [SerializeField]
    private UIUpgradeDisplay upgradeDisplayPrefab;
    
    [SerializeField]
    private Transform upgradeDisplayParent;
    
    [SerializeField]
    private UIFlexibleLayoutGroup flexibleLayoutGroup;

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
    
    private readonly List<UIUpgradeDisplay> spawnedDisplays = new List<UIUpgradeDisplay>();

    private DistrictData districtData;
    private Canvas canvas;
    private Camera cam;

    private void OnEnable()
    {
        canvas = GetComponentInParent<Canvas>();
        cam = Camera.main;

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
        PositionRectTransform.PositionOnOverlayCanvas(canvas, cam, transform as RectTransform, districtData.Position, pivot);
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
        this.districtData = districtData;

        parentPanel.SetActive(true);
        ownedEffectsPanel.SpawnEffects();
        (ownedEffectsPanel.transform.parent as RectTransform).anchoredPosition = Vector2.zero;

        ShowSection(true);
        
        SpawnUpgradeDisplays(districtData);
        panelTitleText.text = districtData.State switch
        {
            TownHallState => "Town Hall",
            MineState => "Mine District",
            BombState => "Bomb District",
            ArcherState => "Archer District",

            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private void SpawnUpgradeDisplays(DistrictData districtData)
    {
        for (int i = 0; i < districtData.UpgradeStats.Count; i++)
        {
            UIUpgradeDisplay spawned = upgradeDisplayPrefab.Get<UIUpgradeDisplay>();
            spawned.DistrictUpgrade = this;
            spawned.DisplayStat(districtData.UpgradeStats[i]);
            spawned.transform.SetParent(upgradeDisplayParent, false);
            spawned.transform.SetSiblingIndex(i);
            spawnedDisplays.Add(spawned);
        }
        
        flexibleLayoutGroup.coloumns = districtData.UpgradeStats.Count;
        flexibleLayoutGroup.CalculateNewBounds();
        
        DisplayUpgrade(districtData.UpgradeStats[0]);
    }

    public void DisplayUpgrade(IUpgradeStat stat)
    {
        upgradeTitleText.text = stat.Name;

        for (int i = 0; i < descriptions.Length; i++)
        {
            if (i >= stat.Descriptions.Length)
            {
                descriptions[i].text = "";
                points[i].gameObject.SetActive(false);
            }
            else
            {
                descriptions[i].text = string.Format(stat.Descriptions[i], i == 1 ? stat.Value.ToString("N") : stat.GetIncrease().ToString("N"));
                points[i].gameObject.SetActive(true);
            }
        }

        costText.text = $"Cost: {stat.GetCost()}";
    }

    public void Close()
    {
        for (int i = 0; i < spawnedDisplays.Count; i++)
        {
            spawnedDisplays[i].gameObject.SetActive(false);
        }

        parentPanel.SetActive(false);
    }
    #endregion

    #region Functionality

    public async UniTaskVoid UpgradeStat(IUpgradeStat stat)
    {
        float cost = stat.GetCost();
        bool succeded = await stat.IncreaseLevel();
        if (!succeded)
        {
            return;
        }

        MoneyManager.Instance.RemoveMoney(cost);

        districtData.LevelUp();

        DisplayUpgrade(stat);
    }

    public bool CanPurchase(IUpgradeStat upgradeStat)
    {
        return MoneyManager.Instance.Money >= upgradeStat.GetCost();
    }

    private void AddEffectToTower(EffectModifier effectModifier)
    {
        districtData.State.Attack.AddEffect(effectModifier.Effects, effectModifier.EffectType);
    }

    private void RemoveEffectFromTower(EffectModifier modifier)
    {
        districtData.State.Attack.RemoveEffect(modifier.Effects, modifier.EffectType);
    }

    #endregion
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
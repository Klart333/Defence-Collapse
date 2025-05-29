using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Buildings.District;
using UnityEngine.UI;
using Gameplay.Money;
using UnityEngine;
using DG.Tweening;
using System;
using InputCamera;
using Loot;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

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
    
    private readonly Dictionary<DistrictData, List<EffectModifier>> effectModifiers = new Dictionary<DistrictData, List<EffectModifier>>();
    
    private readonly List<EffectModifier> availableEffectModifiers = new List<EffectModifier>();
    private readonly List<UIUpgradeDisplay> spawnedDisplays = new List<UIUpgradeDisplay>();

    private DistrictUpgradeManager upgradeManager;
    private InputManager inputManager;
    private DistrictData districtData;
    private Canvas canvas;
    private Camera cam;
    
    private void OnEnable()
    {
        canvas = GetComponentInParent<Canvas>();
        cam = Camera.main;

        towerEffectsPanel.OnEffectRemoved += RemoveEffectFromTower;
        towerEffectsPanel.OnEffectAdded += AddEffectToTower;
        
        GetUpgradeManager().Forget();
        GetInput().Forget();
    }

    private async UniTaskVoid GetUpgradeManager()
    {
        upgradeManager = await DistrictUpgradeManager.Get();
        upgradeManager.OnEffectGained += OnEffectGained;
    }
    
    private async UniTaskVoid GetInput()
    {
        inputManager = await InputManager.Get();
        inputManager.Fire.canceled += ClickReleased;
    }

    private void OnDisable()
    {
        towerEffectsPanel.OnEffectRemoved -= RemoveEffectFromTower;
        towerEffectsPanel.OnEffectAdded -= AddEffectToTower;
        upgradeManager.OnEffectGained -= OnEffectGained;
        inputManager.Fire.canceled -= ClickReleased;
    }
    
    private void Update()
    {
        if (parentPanel.activeSelf)
        {
            PositionRectTransform.PositionOnOverlayCanvas(canvas, cam, parentPanel.transform as RectTransform, districtData.Position, pivot);
        }
    }
    
    private void ClickReleased(InputAction.CallbackContext obj)
    {
        if (parentPanel.activeSelf)
        {
            CheckCancel();
        }
    }

    private void CheckCancel()
    {
        if (InputManager.Instance.Fire.WasReleasedThisFrame() && !CameraController.IsDragging && !EventSystem.current.IsPointerOverGameObject())
        {
            UIEvents.OnFocusChanged?.Invoke();
        }
    }

    #region UI

    public void ShowSection(bool isUpgrade)
    {
        upgradeSection.SetActive(isUpgrade);
        modifySection.SetActive(!isUpgrade);

        line.DOKill();
        line.DOAnchorPosX(isUpgrade ? -100 : 100, 0.2f).SetEase(Ease.OutCirc);

        (ownedEffectsPanel.transform.parent as RectTransform).DOAnchorPosX(isUpgrade ? -0 : -420, 0.5f).SetEase(Ease.OutCirc);
        
        if (!isUpgrade && effectModifiers.TryGetValue(districtData, out List<EffectModifier> effects))
        {
            towerEffectsPanel.SpawnEffects(effects);
        }
    }

    public void ShowUpgrades(DistrictData districtData)
    {
        this.districtData = districtData;

        parentPanel.SetActive(true);
        ownedEffectsPanel.SpawnEffects(availableEffectModifiers);
        
        (ownedEffectsPanel.transform.parent as RectTransform).anchoredPosition = Vector2.zero;

        ShowSection(true);
        
        SpawnUpgradeDisplays(districtData);
        panelTitleText.text = districtData.State switch
        {
            TownHallState => "Town Hall",
            MineState => "Mine District",
            BombState => "Bomb District",
            ArcherState => "Archer District",
            FlameState => "Flame District",
            LightningState => "Lightning District",

            _ => throw new ArgumentOutOfRangeException()
        };
        
        districtData.OnDisposed += DistrictDataOnOnDisposed;

        void DistrictDataOnOnDisposed()
        {
            districtData.OnDisposed -= DistrictDataOnOnDisposed;

            if (effectModifiers.TryGetValue(districtData, out List<EffectModifier> districtEffects))
            {
                availableEffectModifiers.AddRange(districtEffects);     
            }
            
            parentPanel.SetActive(false);
        }
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
        if (!parentPanel.activeSelf)
        {
            return;
        }
        
        for (int i = 0; i < spawnedDisplays.Count; i++)
        {
            spawnedDisplays[i].gameObject.SetActive(false);
        }

        parentPanel.SetActive(false);
    }
    #endregion

    #region Upgrades

    public void UpgradeStat(IUpgradeStat stat)
    {
        float cost = stat.GetCost();
        stat.IncreaseLevel();

        MoneyManager.Instance.RemoveMoney(cost);

        districtData.LevelUp();

        DisplayUpgrade(stat);
    }

    public bool CanPurchase(IUpgradeStat upgradeStat)
    {
        return MoneyManager.Instance.Money >= upgradeStat.GetCost();
    }

    #endregion
    
    #region Effects
    
    private void OnEffectGained(EffectModifier effectModifier)
    {
        availableEffectModifiers.Add(effectModifier);
        if (gameObject.activeInHierarchy)
        {
            Close();
        }
    }
    
    private void AddEffectToTower(EffectModifier effectModifier)
    {
        districtData.State.Attack.AddEffect(effectModifier.Effects, effectModifier.EffectType);

        availableEffectModifiers.Remove(effectModifier);

        if (effectModifiers.TryGetValue(districtData, out List<EffectModifier> list))
        {
            list.Add(effectModifier);
        }
        else
        {
            effectModifiers.Add(districtData, new List<EffectModifier> { effectModifier });
        }

    }

    private void RemoveEffectFromTower(EffectModifier effectModifier)
    {
        districtData.State.Attack.RemoveEffect(effectModifier.Effects, effectModifier.EffectType);
        
        availableEffectModifiers.Add(effectModifier);
        
        if (!effectModifiers.TryGetValue(districtData, out List<EffectModifier> list)) return;
        list.Remove(effectModifier);
        if (list.Count == 0)
        {
            effectModifiers.Remove(districtData);
        }
    }

    #endregion
}

public static class PositionRectTransform // Chat gippity
{
    public static void PositionOnOverlayCanvas(Canvas canvas, Camera cam, RectTransform rectTransform, Vector3 worldPosition, Vector2 pivot)
    {
        // Set anchored position
        rectTransform.anchoredPosition = GetPositionOnOverlayCanvas(canvas, cam, rectTransform, worldPosition, pivot);
    }
    
    // Function to position RectTransform on Overlay Canvas to align with world position
    public static Vector2 GetPositionOnOverlayCanvas(Canvas canvas, Camera cam, RectTransform rectTransform, Vector3 worldPosition, Vector2 pivot)
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

        // Return anchored position
        return canvasPos + pivotOffset;
    }
}
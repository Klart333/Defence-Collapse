using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIUpgradeDisplay : MonoBehaviour
{
    [Title("Stat Type")]
    [SerializeField]
    private LevelStat upgradeType;

    [Title("Upgrade Info")]
    [SerializeField]
    private string upgradeName;

    [SerializeField]
    private string description;

    [SerializeField]
    private string currentDescription;

    [Title("References")]
    [SerializeField]
    private UIBuildingUpgrade buildingUpgrade;

    [SerializeField]
    private StupidButton button;

    private BuildingData currentData;

    private bool hoveredLastFrame = false;

    public void DisplayStat(BuildingData buildingData)
    {
        currentData = buildingData;

        UpdateButton();

        MoneyManager.Instance.OnMoneyChanged += UpdateTheButton;
    }

    public void Close()
    {
        MoneyManager.Instance.OnMoneyChanged -= UpdateTheButton;
    }

    private void UpdateTheButton(float _)
    {
        UpdateButton();
    }

    private async void UpdateButton()
    {
        if (button.interactable)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
        }
        
        button.interactable = buildingUpgrade.CanPurchase(upgradeType);
    }

    public void ClickUpgrade()
    {
        if (buildingUpgrade.CanPurchase(upgradeType))
        {
            buildingUpgrade.UpgradeStat(upgradeType);
        }
    }

    private void Update()
    {
        if (!hoveredLastFrame && button.Hovered)
        {
            DisplayUpgrade();
        }

        hoveredLastFrame = button.Hovered;
    }

    public void DisplayUpgrade()
    {
        List<string> descriptions = new List<string>();

        descriptions.Add(string.Format(description, buildingUpgrade.GetIncrease(upgradeType, currentData.UpgradeData.GetStatLevel(upgradeType))));
        switch (upgradeType)
        {
            case LevelStat.AttackSpeed:
                descriptions.Add(string.Format(currentDescription, currentData.State.Stats.AttackSpeed.Value));
                break;
            case LevelStat.Damage:
                descriptions.Add(string.Format(currentDescription, currentData.State.Stats.DamageMultiplier.Value));
                break;
            case LevelStat.Range:
                descriptions.Add(string.Format(currentDescription, currentData.State.Range));
                break;
            default:
                break;
        }

        buildingUpgrade.DisplayUpgrade(upgradeName, descriptions, upgradeType);
    }
}
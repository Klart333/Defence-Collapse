﻿using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

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
        List<string> descriptions = new List<string>
        {
            string.Format(description, buildingUpgrade.LevelData.GetIncrease(upgradeType, currentData.UpgradeData.GetStatLevel(upgradeType)))
        };

        float value = 0;
        switch (upgradeType)
        {
            case LevelStat.AttackSpeed:
                value = Mathf.RoundToInt(currentData.State.Stats.AttackSpeed.Value * 10f) / 10f;
                break;
            case LevelStat.Damage:
                value = Mathf.RoundToInt(currentData.State.Stats.DamageMultiplier.Value * 10f) / 10f;
                break;
            case LevelStat.Range:
                value = Mathf.RoundToInt(currentData.State.Range * 10f) / 10f;
                break;
            default:
                break;
        }

        descriptions.Add(string.Format(currentDescription, value));


        buildingUpgrade.DisplayUpgrade(upgradeName, descriptions, upgradeType);
    }
}
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using Buildings.District;
using UnityEngine;
using UnityEngine.Serialization;

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

    [FormerlySerializedAs("buildingUpgrade")]
    [Title("References")]
    [SerializeField]
    private UIDistrictUpgrade districtUpgrade;

    [SerializeField]
    private StupidButton button;

    private DistrictData currentData;

    private bool hoveredLastFrame = false;

    public void DisplayStat(DistrictData buildingData)
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
        
        button.interactable = districtUpgrade.CanPurchase(upgradeType);
    }

    public void ClickUpgrade()
    {
        if (districtUpgrade.CanPurchase(upgradeType))
        {
            districtUpgrade.UpgradeStat(upgradeType);
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
            string.Format(description, districtUpgrade.LevelData.GetIncrease(upgradeType, currentData.UpgradeData.GetStatLevel(upgradeType)))
        };

        float value = upgradeType switch
        {
            LevelStat.AttackSpeed => Mathf.RoundToInt(currentData.State.Stats.AttackSpeed.Value * 10) / 10f,
            LevelStat.Damage => Mathf.RoundToInt(currentData.State.Stats.DamageMultiplier.Value * 10) / 10f,
            LevelStat.Range => Mathf.RoundToInt(currentData.State.Range * 10) / 10f,
            _ => 0
        };

        descriptions.Add(string.Format(currentDescription, value));

        districtUpgrade.DisplayUpgrade(upgradeName, descriptions, upgradeType);
    }
}
using System.Collections.Generic;
using UnityEngine.Serialization;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using System;
using Gameplay.Money;
using UnityEngine.UI;

public class UIUpgradeDisplay : PooledMonoBehaviour
{
    [Title("References")]
    [SerializeField]
    private StupidButton button;

    [SerializeField]
    private Image iconImage;
    
    private UpgradeStat upgradeStat;

    private bool hoveredLastFrame = false;
    
    public UIDistrictUpgrade DistrictUpgrade { get; set; }
    
    protected override void OnDisable()
    {
        base.OnDisable();

        MoneyManager.Instance.OnMoneyChanged -= UpdateTheButton;
    }

    private void Update()
    {
        if (!hoveredLastFrame && button.Hovered)
        {
            DistrictUpgrade.DisplayUpgrade(upgradeStat);
        }

        hoveredLastFrame = button.Hovered;
    }
    
    private void UpdateTheButton(float _)
    {
        UpdateButton();
    }

    public void DisplayStat(UpgradeStat stat)
    {
        upgradeStat = stat;
        iconImage.sprite = upgradeStat.Icon;

        UpdateButton();

        MoneyManager.Instance.OnMoneyChanged += UpdateTheButton;
    }
    
    private void UpdateButton()
    {
        button.interactable = DistrictUpgrade.CanPurchase(upgradeStat);
    }

    public void ClickUpgrade()
    {
        if (DistrictUpgrade.CanPurchase(upgradeStat))
        {
            DistrictUpgrade.UpgradeStat(upgradeStat);
        }
    }
}
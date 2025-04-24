using System.Collections.Generic;
using UnityEngine.Serialization;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using System;

public class UIUpgradeDisplay : PooledMonoBehaviour
{
    [Title("Upgrade Info")]
    [SerializeField]
    private string description;

    [SerializeField]
    private string currentDescription;

    [Title("References")]
    [SerializeField]
    private StupidButton button;
    
    private UpgradeStat upgradeStat;

    private bool hoveredLastFrame = false;
    
    public UIDistrictUpgrade DistrictUpgrade { get; set; }

    public void DisplayStat(UpgradeStat stat)
    {
        upgradeStat = stat;

        UpdateButton();

        MoneyManager.Instance.OnMoneyChanged += UpdateTheButton;
    }

    protected override void OnDisable()
    {
        base.OnDisable();

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
        
        button.interactable = DistrictUpgrade.CanPurchase(upgradeStat);
    }

    public void ClickUpgrade()
    {
        if (DistrictUpgrade.CanPurchase(upgradeStat))
        {
            DistrictUpgrade.UpgradeStat(upgradeStat);
        }
    }

    private void Update()
    {
        if (!hoveredLastFrame && button.Hovered)
        {
            DistrictUpgrade.DisplayUpgrade(upgradeStat);
        }

        hoveredLastFrame = button.Hovered;
    }
}
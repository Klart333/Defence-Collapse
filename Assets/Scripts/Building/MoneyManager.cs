using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

public class MoneyManager : Singleton<MoneyManager>
{
    public event Action<float> OnMoneyChanged;

    [Title("Money")]
    [SerializeField]
    private int startingMoney = 10;

    [ReadOnly, SerializeField]
    private float money = 0;

    [Title("Building Info")]
    [SerializeField]
    private BuildableCostData costData;

    private Dictionary<BuildingType, int> AvailableBuildables = new Dictionary<BuildingType, int>();

    private BuildingType currentBuildingType;

    private bool purchasing = false;

    public float Money => money;

    private void OnEnable()
    {
        AvailableBuildables = new Dictionary<BuildingType, int>
        {
            { BuildingType.Castle, 1 },
            { BuildingType.Building, 0 },
            { BuildingType.Path, 0 },
        };
        money = startingMoney;

        Events.OnBuildingClicked += TowerClicked;
        Events.OnBuildingPurchased += StartPurchase;
        Events.OnBuildingBuilt += BuilableBuilt;

        Events.OnBuildingCanceled += CancelPurchasing;
    }

    private void OnDisable()
    {
        Events.OnBuildingClicked -= TowerClicked;
        Events.OnBuildingPurchased -= StartPurchase;
        Events.OnBuildingBuilt -= BuilableBuilt;

        Events.OnBuildingCanceled -= CancelPurchasing;
    }

    private void CancelPurchasing()
    {
        purchasing = false;
    }

    private void TowerClicked(BuildingType buildingType)
    {
        if (purchasing) return;

        if (CanPurchase(buildingType))
        {
            Events.OnBuildingPurchased?.Invoke(buildingType);
        }
    }

    private bool CanPurchase(BuildingType buildingType)
    {
        if (AvailableBuildables[buildingType] > 0)
        {
            return true;
        }

        if (Money >= costData.GetCost(buildingType))
        {
            return true;
        }

        return false;
    }

    private void StartPurchase(BuildingType buildingType)
    {
        currentBuildingType = buildingType;
        purchasing = true;
    }

    private void BuilableBuilt(IEnumerable<IBuildable> buildables)
    {
        if (AvailableBuildables[currentBuildingType] > 0)
        {
            AvailableBuildables[currentBuildingType] -= 1;
        }
        else
        {
            RemoveMoney(costData.GetCost(currentBuildingType));
        }

        if (!CanPurchase(currentBuildingType))
        {
            Events.OnBuildingCanceled?.Invoke();
        }
    }

    #region Public

    public void AddBuildable(BuildingType buildingType, int amount)
    {
        AvailableBuildables[buildingType] += amount;
    }

    public void AddMoney(float amount)
    {
        money += amount;

        OnMoneyChanged?.Invoke(money);
    }

    public void RemoveMoney(float amount)
    {
        money -= amount;

        OnMoneyChanged?.Invoke(money);
    }

    #endregion
}

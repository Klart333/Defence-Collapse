using System;
using UnityEngine;

public class MoneyManager : MonoBehaviour
{
    private BuildingPurchaser towerPurchaser;

    private float moneyAmount = 0;
    private bool purchasing = false;

    public float MoneyAmount => moneyAmount;

    private void Start()
    {
        towerPurchaser = new BuildingPurchaser(this);

        Events.OnBuildingClicked += TowerClicked;
        Events.OnBuildingPurchased += TowerPurchased;
        Events.OnBuildingCanceled += CancelPurchasing;
    }

    private void OnDestroy()
    {
        Events.OnBuildingClicked -= TowerClicked;
        Events.OnBuildingPurchased -= TowerPurchased;

        Events.OnBuildingCanceled -= CancelPurchasing;
    }

    private void CancelPurchasing()
    {
        purchasing = false;
    }

    private void TowerClicked(BuildingType buildingType)
    {
        if (purchasing) return;

        Events.OnBuildingPurchased?.Invoke(buildingType);

        //if (!purchasing && towerPurchaser.CanPurchaseBuilding(tower))
        //{
        //} 
    }

    private void TowerPurchased(BuildingType buildingType)
    {
        //moneyAmount -= tower.Cost;

        purchasing = true;
    }
}

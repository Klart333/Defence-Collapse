﻿using System;
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
        Events.OnBuildingBuilt += CancelPurchasing;
        Events.OnBuildingCanceled += CancelPurchasing;
    }

    private void OnDestroy()
    {
        Events.OnBuildingClicked -= TowerClicked;
        Events.OnBuildingPurchased -= TowerPurchased;
        Events.OnBuildingBuilt -= CancelPurchasing;
        Events.OnBuildingCanceled -= CancelPurchasing;
    }

    private void CancelPurchasing(Building arg0)
    {
        purchasing = false;
    }

    private void TowerClicked(Building tower)
    {
        if (!purchasing && towerPurchaser.CanPurchaseBuilding(tower))
        {
            Events.OnBuildingPurchased.Invoke(tower);
        } 
    }

    private void TowerPurchased(Building tower)
    {
        moneyAmount -= tower.Cost;

        purchasing = true;
    }
}

using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using Buildings.District;
using Gameplay;
using Juice;
using Unity.Mathematics;
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
    
    [Title("District Info")]
    [SerializeField]
    private DistrictCostUtility districtCostUtility;
    
    [Title("Visual")]
    [SerializeField]
    private CrystalParticleHandler particleHandler;

    private Dictionary<BuildingType, int> AvailableBuildables = new Dictionary<BuildingType, int>();
    

    public float Money => money;

    private void OnEnable()
    {
        AvailableBuildables = new Dictionary<BuildingType, int>
        {
            { BuildingType.Building, 0 },
            { BuildingType.Path, 0 },
        };
        money = startingMoney;
    }
    
    #region Public

    public bool CanPurchase(BuildingType buildingType)
    {
        if (AvailableBuildables[buildingType] > 0)
        {
            return true;
        }

        if (Money >= costData.GetCost(buildingType))
        {
            return true;
        }

        Debug.Log("Tell player not enough money");
        return false;
    }
    
    
    public bool CanPurchase(DistrictType districtType, int chunkAmount, out float cost)
    {
        cost = districtCostUtility.GetCost(districtType, chunkAmount);
        if (Money >= cost)
        {
            return true;
        }

        InsufficientFunds(cost);
        return false;
    }
    
    public void Purchase(BuildingType buildingType)
    {
        if (AvailableBuildables[buildingType] > 0)
        {
            AvailableBuildables[buildingType] -= 1;
        }
        else
        {
            RemoveMoney(costData.GetCost(buildingType));
        }
    }
    
    public void Purchase(DistrictType districtType, int chunkAmount)
    {
        float cost = districtCostUtility.GetCost(districtType, chunkAmount);

        RemoveMoney(cost);
    }
    
    public void AddBuildable(BuildingType buildingType, int amount)
    {
        AvailableBuildables[buildingType] += amount;
    }

    public void AddMoney(float amount)
    {
        money += amount;

        OnMoneyChanged?.Invoke(money);
    }

    public void AddMoneyParticles(float amount, Vector3 position)
    {
        particleHandler.CollectCrystals((int)amount, position).Forget();
    }

    public void RemoveMoney(float amount)
    {
        money -= amount;

        OnMoneyChanged?.Invoke(money);
    }

    public void InsufficientFunds(float cost)
    {
        Debug.Log("Insufficient funds, " + (Money - cost));
    }

    #endregion

    #region Debug

    [Button]
    private void AddMoneyDebug(float money)
    {
        this.money += money;
    }

    #endregion

}

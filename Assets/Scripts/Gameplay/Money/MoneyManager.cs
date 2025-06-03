using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;
using System;
using Juice;

namespace Gameplay.Money
{
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

        [Title("Debug")]
        [SerializeField]
        private bool verbose = true;
        
        private EntityManager entityManager;
        private Entity moneyEntity;

        public int BuildingCost => costData.GetCost(BuildingType.Building);
        public float PathCost => costData.GetCost(BuildingType.Path);

        public float Money => money;

        private void OnEnable()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            moneyEntity = entityManager.CreateEntity(); 
            entityManager.AddComponent<MoneyToAddComponent>(moneyEntity);
            
            money = startingMoney;
        }
        
        private void Update()
        {
            if (GameManager.Instance.IsGameOver)
            {
                return;
            }
            
            float amount = entityManager.GetComponentData<MoneyToAddComponent>(moneyEntity).Money;
            if (amount > 0)
            {
                AddMoney(amount);
                entityManager.SetComponentData(moneyEntity, new MoneyToAddComponent { Money = 0 });
            }
            
            if (Input.GetKeyDown(KeyCode.M))
            {
                AddMoneyDebug();
            }
        }
        
        #region Public

        public bool CanPurchase(BuildingType buildingType)
        {
            if (Money >= costData.GetCost(buildingType))
            {
                return true;
            }

            Debug.Log("Tell player not enough money");
            return false;
        }


        public bool CanPurchase(DistrictType districtType, int districtAmount, int extraBuildingAmount, out float cost)
        {
            cost = districtCostUtility.GetCost(districtType, districtAmount);
            cost += extraBuildingAmount * BuildingCost;
            if (Money >= cost)
            {
                return true;
            }

            InsufficientFunds(cost);
            return false;
        }

        public void Purchase(BuildingType buildingType)
        {
            RemoveMoney(costData.GetCost(buildingType));
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
            if (verbose)
            {
                Debug.Log($"Removing {amount} money");
            }
            
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
        private void AddMoneyDebug(float money = 1000)
        {
            this.money += money;
        }

        #endregion

    }

    public struct MoneyToAddComponent : IComponentData
    {
        public float Money;
    }
}

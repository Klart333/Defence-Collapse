using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;
using System;
using Buildings;
using Effects;
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
        
        public float Money => money;
        public Stat MoneyMultiplier { get; private set; }

        private void OnEnable()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            moneyEntity = entityManager.CreateEntity(); 
            entityManager.AddComponent<MoneyToAddComponent>(moneyEntity);

            MoneyMultiplier = new Stat(1);
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

        public bool CanPurchase(DistrictType districtType, int districtAmount, out float cost)
        {
            cost = districtCostUtility.GetCost(districtType, districtAmount);
            if (Money >= cost)
            {
                return true;
            }

            InsufficientFunds(cost);
            return false;
        }
        
        public void AddMoney(float amount, bool addMultiplier = true)
        {
            if (addMultiplier)
            {
                amount *= MoneyMultiplier.Value;
            }
            money += amount;

            OnMoneyChanged?.Invoke(money);
        }

        public float AddMoneyParticles(float amount, Vector3 position)
        {
            amount *= MoneyMultiplier.Value;
            particleHandler.CollectCrystals((int)amount, position).Forget();

            return amount;
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
        
        #region Debug

        [Button]
        private void AddMoneyDebug(float money = 100000)
        {
            AddMoney(money);
        }

        #endregion

    }

    public struct MoneyToAddComponent : IComponentData
    {
        public float Money;
    }
}

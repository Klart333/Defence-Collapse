using System.Collections.Generic;
using Sirenix.OdinInspector;
using Gameplay.Upgrades.ECS;
using Gameplay.Upgrades;
using Unity.Entities;
using Effects.ECS;
using UnityEngine;
using Health;
using System;

namespace Gameplay
{
    public class GameDataManager : Singleton<GameDataManager>
    {
        [Title("Fire")]
        [SerializeField]
        private FireTickDataComponent defaultFireTickData;
        
        [Title("Poison")]
        [SerializeField]
        private PoisonTickDataComponent defaultPoisonTickData;
        
        private Dictionary<Tuple<CategoryType, HealthType>, MultiplyDamageComponent> multiplyDamageComponents = new Dictionary<Tuple<CategoryType, HealthType>, MultiplyDamageComponent>(); 
        private Dictionary<Tuple<CategoryType, HealthType>, Entity> multiplyDamageEntities = new Dictionary<Tuple<CategoryType, HealthType>, Entity>();
            
        private EntityManager entityManager;
        private Entity fireDataEntity;
        private Entity poisonDataEntity;
        
        private FireTickDataComponent fireTickData;
        private PoisonTickDataComponent poisonTickData;
        
        protected override void Awake()
        {
            base.Awake();

            InitailizeGameData();
        }

        private void InitailizeGameData()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            fireDataEntity = entityManager.CreateEntity();
            fireTickData = new FireTickDataComponent
            {
                TickRate = defaultFireTickData.TickRate,
                TickDamage = defaultFireTickData.TickDamage,
            };
            entityManager.AddComponentData(fireDataEntity, fireTickData);
            
            poisonDataEntity = entityManager.CreateEntity();
            poisonTickData = new PoisonTickDataComponent
            {
                TickRate = defaultPoisonTickData.TickRate,
                TickDamage = defaultPoisonTickData.TickDamage,
            };
            entityManager.AddComponentData(poisonDataEntity, poisonTickData);
        }

        public void IncreaseGameData(IComponentData componentData)
        {
            switch (componentData)
            {
                case FireTickDataComponent fireTickDataIncrease:
                    fireTickData.TickDamage += fireTickDataIncrease.TickDamage;
                    fireTickData.TickRate = 1.0f / ((1.0f / fireTickData.TickRate) * fireTickDataIncrease.TickRate);
                    entityManager.SetComponentData(fireDataEntity, fireTickData);
                    break;
                
                case PoisonTickDataComponent poisonTickDataIncrease:
                    poisonTickData.TickDamage += poisonTickDataIncrease.TickDamage;
                    poisonTickData.TickRate = 1.0f / ((1.0f / poisonTickData.TickRate) * poisonTickDataIncrease.TickRate);
                    entityManager.SetComponentData(poisonDataEntity, poisonTickData);
                    break;
                case MultiplyDamageComponent damageComponent:
                    AddMultiplyDamageComponent(damageComponent);
                    break;
            }
        }

        private void AddMultiplyDamageComponent(MultiplyDamageComponent damageComponent)
        {
            Tuple<CategoryType, HealthType> key = Tuple.Create(damageComponent.AppliedCategory, damageComponent.AppliedHealthType);
            if (multiplyDamageComponents.TryGetValue(key, out MultiplyDamageComponent multiplyDamageComponent))
            {
                multiplyDamageComponent.DamageMultiplier *= damageComponent.DamageMultiplier;
                entityManager.SetComponentData(multiplyDamageEntities[key], multiplyDamageComponent);
            }
            else
            {
                Entity entity = entityManager.CreateEntity();
                entityManager.AddComponentData(entity, damageComponent);
                multiplyDamageComponents.Add(key, damageComponent);
                multiplyDamageEntities.Add(key, entity);
            }
        }
    }
}
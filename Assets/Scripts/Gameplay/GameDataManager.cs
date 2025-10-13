using System.Collections.Generic;
using Sirenix.OdinInspector;
using Gameplay.Upgrades.ECS;
using Gameplay.Upgrades;
using Unity.Entities;
using Gameplay.Event;
using Effects.ECS;
using UnityEngine;
using Enemy.ECS;
using Effects;
using Health;
using System;

namespace Gameplay
{
    public static class GameData
    {
        public static Stat WallHealthMultiplier { get; set; }
        public static Stat WallHealing { get; set; }
        
        public static Stat BarricadeHealthMultiplier { get; private set; }
        public static Stat BarricadeHealing { get; private set; }

        public static void InitializeStats()
        {
            BarricadeHealthMultiplier = new Stat(1);
            WallHealthMultiplier = new Stat(1);
            BarricadeHealing = new Stat(0);
            WallHealing = new Stat(0);
        }

        public static void ResetStats()
        {
            BarricadeHealthMultiplier = null;
            WallHealthMultiplier = null;
            BarricadeHealing = null;
            WallHealing = null;
        }
    }
    
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
        private Entity enemySpeedModifierEntity;
        private Entity enemyDamageModifierEntity;
        
        private FireTickDataComponent fireTickData;
        private PoisonTickDataComponent poisonTickData;
        private EnemySpeedModifierComponent speedComponent;
        private EnemyDamageModifierComponent damageComponent;
        
        protected override void Awake()
        {
            base.Awake();
            
            GameData.InitializeStats();
            InitailizeGameData();
        }

        private void OnEnable()
        {
            Events.OnGameReset += OnGameReset;
        }

        private void OnDisable()
        {
            Events.OnGameReset -= OnGameReset;
        }

        private void OnGameReset()
        {
            GameData.ResetStats();
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
                
                case MultiplyDamageComponent multiplyDamageComponent:
                    AddMultiplyDamageComponent(multiplyDamageComponent);
                    break;
                
                case EnemySpeedModifierComponent speedModifierComponent:
                    if (entityManager.Exists(enemySpeedModifierEntity))
                    {
                        speedComponent.SpeedMultiplier *= speedModifierComponent.SpeedMultiplier;
                        entityManager.SetComponentData(enemySpeedModifierEntity, speedComponent);
                    }
                    else
                    {
                        enemySpeedModifierEntity = entityManager.CreateEntity();
                        speedComponent = speedModifierComponent;
                        entityManager.AddComponentData(enemySpeedModifierEntity, speedComponent);
                    }
                    break;

                case EnemyDamageModifierComponent damageModifierComponent:
                    if (entityManager.Exists(enemyDamageModifierEntity))
                    {
                        damageComponent.DamageMultiplier *= damageModifierComponent.DamageMultiplier;
                        entityManager.SetComponentData(enemyDamageModifierEntity, damageComponent);
                    }
                    else
                    {
                        enemyDamageModifierEntity = entityManager.CreateEntity();
                        damageComponent = damageModifierComponent;
                        entityManager.AddComponentData(enemyDamageModifierEntity, damageComponent);
                    }
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
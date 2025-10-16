using System.Collections.Generic;
using Sirenix.OdinInspector;
using Buildings.District;
using Unity.Collections;
using Gameplay.Event;
using Unity.Entities;
using System.Linq;
using UnityEngine;
using Effects.ECS;
using Effects;
using System;

namespace Gameplay.Upgrades
{
    public class AppliedEffectsHandler : MonoBehaviour
    {
        [Title("References")]
        [SerializeField]
        private DistrictHandler districtHandler;
        
        public readonly Dictionary<UpgradeComponentType, List<CategoryType>> spawnedEffects = new Dictionary<UpgradeComponentType, List<CategoryType>>();
        
        private EntityManager entityManager;
        private EntityQuery upgradeComponentQuery;

        private readonly Dictionary<CategoryType, List<IEffect>> appliedEffects = new Dictionary<CategoryType, List<IEffect>>();
        
        private void OnEnable()
        {
            Events.OnUpgradeCardPicked += PerformUpgrade;
           
            districtHandler.OnDistrictCreated += OnDistrictCreated;
            
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var builder = new EntityQueryBuilder(Allocator.Temp).WithAll<AddComponentComponent>();
            upgradeComponentQuery = builder.Build(entityManager);
            
            builder.Dispose();
        }
        
        private void OnDisable()
        {
            Events.OnUpgradeCardPicked -= PerformUpgrade;
            
            districtHandler.OnDistrictCreated -= OnDistrictCreated;
        }
        
        private void OnDistrictCreated(DistrictData district)
        {
            if (appliedEffects.TryGetValue(CategoryType.AllDistrict, out List<IEffect> effects))
            {
                foreach (IEffect effect in effects)
                {
                    effect.Perform(district.State);
                }
            }
            
            if (appliedEffects.TryGetValue(district.State.CategoryType, out List<IEffect> moreEffects))
            {
                foreach (IEffect effect in moreEffects)
                {
                    effect.Perform(district.State);
                }
            }
        }

        private void PerformUpgrade(UpgradeCardData.UpgradeCardInstance upgradeInstance)
        {
            switch (upgradeInstance.UpgradeType)
            {
                case UpgradeType.Effect:
                    foreach (IEffect effect in upgradeInstance.Effects)
                    {
                        AddUpgradeEffect(upgradeInstance.AppliedCategories, effect);
                    }
                    break;
                
                case UpgradeType.OnDamageEffect:
                    AddUpgradeEffectOnDamage(upgradeInstance.AppliedCategories, upgradeInstance.Effects);
                    break;
                
                case UpgradeType.Component:
                    AddComponentEffect(upgradeInstance.AppliedCategories, upgradeInstance.ComponentType, upgradeInstance.ComponentStrength);
                    break;
                
                case UpgradeType.StandAloneEffect:
                    foreach (IEffect effect in upgradeInstance.Effects)
                    {
                        effect.Perform(null);
                    }
                    break;
            }
        }
        
        public void AddUpgradeEffect(CategoryType appliedDistrict, IEffect effect)
        {
            foreach (DistrictData district in districtHandler.UniqueDistricts.Values)
            {
                if (appliedDistrict.HasFlag(district.State.CategoryType))
                {
                    effect.Perform(district.State);
                }
            }

            Array enumValues = Enum.GetValues(typeof(CategoryType));
            foreach (object enumValue in enumValues)
            {
                CategoryType categoryType = (CategoryType)enumValue;
                if (!appliedDistrict.HasFlag(categoryType)) continue;
                
                if (appliedEffects.TryGetValue(categoryType, out List<IEffect> list)) list.Add(effect);
                else appliedEffects.Add(categoryType, new List<IEffect> { effect });
            }
        }
        
        public void AddUpgradeEffectOnDamage(CategoryType appliedDistrict, List<IEffect> effects)
        {
            foreach (DistrictData district in districtHandler.UniqueDistricts.Values)
            {
                if (appliedDistrict.HasFlag(district.State.CategoryType))
                {
                    district.State.Attack.AddEffect(effects, EffectType.DoneDamage);
                }
            }

            Array enumValues = Enum.GetValues(typeof(CategoryType));
            foreach (object enumValue in enumValues)
            {
                CategoryType categoryType = (CategoryType)enumValue;
                if (!appliedDistrict.HasFlag(categoryType)) continue;
                
                if (appliedEffects.TryGetValue(categoryType, out List<IEffect> list)) list.AddRange(effects);
                else appliedEffects.Add(categoryType, effects);
            }
        }
        
        private void AddComponentEffect(CategoryType appliedCategory, UpgradeComponentType componentType, float strength)
        {
            if (spawnedEffects.TryGetValue(componentType, out List<CategoryType> types) && types.Any(x => (x & appliedCategory) > 0))
            {
                using NativeArray<Entity> array = upgradeComponentQuery.ToEntityArray(Allocator.Temp);
                foreach (Entity entity in array)
                {
                    AddComponentComponent add = entityManager.GetComponentData<AddComponentComponent>(entity);
                    if ((appliedCategory & add.AppliedCategory) == 0)
                    {
                        continue;
                    }
                    
                    add.Strength += strength;
                    appliedCategory -= appliedCategory & add.AppliedCategory;
                    entityManager.SetComponentData(entity, add);
                    
                    if (appliedCategory == 0)
                    {
                        return;
                    }
                }
            }

            Entity spawned = entityManager.CreateEntity();
            entityManager.AddComponentData(spawned, new AddComponentComponent
            {
                AppliedCategory = appliedCategory, 
                ComponentType = componentType,
                Strength = strength
            });
            
            if (spawnedEffects.TryGetValue(componentType, out List<CategoryType> list))
            {
                list.Add(appliedCategory);
            }
            else
            {
                spawnedEffects.Add(componentType, new List<CategoryType> { appliedCategory });
            }
        }
    }
}
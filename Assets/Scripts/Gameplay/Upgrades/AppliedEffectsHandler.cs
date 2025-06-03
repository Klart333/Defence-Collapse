using System.Collections.Generic;
using Sirenix.OdinInspector;
using Buildings.District;
using Unity.Entities;
using System.Linq;
using UnityEngine;
using Effects.ECS;
using Effects;
using Unity.Collections;

namespace Gameplay.Upgrades
{
    public class AppliedEffectsHandler : MonoBehaviour
    {
        [Title("References")]
        [SerializeField]
        private DistrictHandler districtHandler;
        
        [SerializeField]
        private UpgradeCardDataUtility upgradeUtility;
        
        public readonly Dictionary<UpgradeComponentType, List<CategoryType>> spawnedEffects = new Dictionary<UpgradeComponentType, List<CategoryType>>();
        
        private EntityManager entityManager;
        private EntityQuery upgradeComponentQuery;

        private readonly Dictionary<CategoryType, List<IEffect>> appliedEffects = new Dictionary<CategoryType, List<IEffect>>();
        
        private void OnEnable()
        {
            foreach (UpgradeCardData data in upgradeUtility.UpgradeCards)
            {
                data.OnUpgradePerformed += PerformUpgrade;
            }
            districtHandler.OnDistrictCreated += OnDistrictCreated;
            
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var builder = new EntityQueryBuilder(Allocator.Temp).WithAll<AddComponentComponent>();
            upgradeComponentQuery = builder.Build(entityManager);
            
            builder.Dispose();
        }
        
        private void OnDisable()
        {
            foreach (UpgradeCardData data in upgradeUtility.UpgradeCards)
            {
                data.OnUpgradePerformed -= PerformUpgrade;
            }
            
            districtHandler.OnDistrictCreated -= OnDistrictCreated;
        }

        private void PerformUpgrade(UpgradeCardData upgradeData)
        {
            switch (upgradeData.UpgradeType)
            {
                case UpgradeType.Effect:
                {
                    foreach (IEffect effect in upgradeData.Effects)
                    {
                        AddUpgradeEffect(upgradeData.AppliedCategories, effect);
                    }

                    break;
                }
                case UpgradeType.Component:
                    AddComponentEffect(upgradeData.AppliedCategories, upgradeData.ComponentType, upgradeData.ComponentStrength);
                    break;
                case UpgradeType.StandAloneEffect:
                    foreach (IEffect effect in upgradeData.Effects)
                    {
                        effect.Perform(null);
                    }
                    break;
            }
        }
        
        public void AddUpgradeEffect(CategoryType appliedDistrict, IEffect effect)
        {
            foreach (DistrictData district in districtHandler.Districts)
            {
                if ((appliedDistrict & (CategoryType.AllDistrict | district.State.CategoryType)) > 0)
                {
                    effect.Perform(district.State);
                }
            }
            
            if (appliedEffects.TryGetValue(appliedDistrict, out List<IEffect> list))
            {
                list.Add(effect);
            }
            else
            {
                appliedEffects.Add(appliedDistrict, new List<IEffect> { effect });
            }
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
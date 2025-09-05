using Unity.Mathematics;
using Unity.Entities;
using Effects.ECS;
using UnityEngine;
using Enemy.ECS;

namespace Effects.LittleDudes
{
    public class LittleDudeAuthoring : MonoBehaviour
    {
        [SerializeField]
        private Stats stats;
        
        private class LittleDudeAuthoringBaker : Baker<LittleDudeAuthoring>
        {
            public override void Bake(LittleDudeAuthoring authoring)
            {
                Entity prefab = GetEntity(authoring, TransformUsageFlags.Dynamic);
                
                AddComponent(prefab, new LittleDudeComponent());
                AddComponent(prefab, new RandomComponent());
                AddComponent(prefab, new ReloadHitsComponent { MaxHitAmount = 1, ReloadInterval = 1.0f / authoring.stats.AttackSpeed });
                AddComponent(prefab, new SpeedComponent { Speed = authoring.stats.MovementSpeed.Value, });
                AddComponent(prefab, new ColliderComponent { Radius = 0.5f });
                
                AddComponent(prefab, new CritComponent
                {
                    CritChance = authoring.stats.CritChance.Value,
                    CritDamage = authoring.stats.CritMultiplier.Value,
                });
                
                AddComponent(prefab, new DamageComponent
                {
                    HealthDamage = authoring.stats.HealthDamage.Value,
                    ArmorDamage = authoring.stats.ArmorDamage.Value,
                    ShieldDamage = authoring.stats.ArmorDamage.Value,
                    
                    IsOneShot = false,
                    TriggerDamageDone = true,
                    LimitedHits = 1,
                    HasLimitedHits = true
                });
                
                AddComponent(prefab, new FlowFieldComponent
                {
                });
                
            }
        }
    }

    public struct LittleDudePrefabComponent : IComponentData
    {
        public Entity Prefab;
    } 
}
using Random = Unity.Mathematics.Random;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Entities;
using Effects.ECS;
using UnityEngine;
using Enemy.ECS;
using Health;
using VFX.ECS;
using HealthComponent = Effects.ECS.HealthComponent;

namespace Enemy
{
    public class Enemy : MonoBehaviour
    {
        [Title("Authoring", "Data")]
        [SerializeField]
        private EnemyData enemyData;
        
        [SerializeField]
        private HealthbarAuthoring healthbar;

        [Title("Authoring", "Movement")]
        [SerializeField]
        private float turnSpeed = 2;
        
        [SerializeField]
        private LayerMask groundMask;

        public EnemyData EnemyData => enemyData;
        
        public class EnemyBaker : Baker<Enemy>
        {
            public override void Bake(Enemy authoring)
            {
                if (authoring.enemyData == null)
                {
                    return;
                }
                
                Stats stats = new Stats(authoring.enemyData.Stats);
                Entity enemyEntity = GetEntity(TransformUsageFlags.Dynamic);
                
                AddComponent<EnemySpawnedTag>(enemyEntity);
                AddComponent<ManagedClusterComponent>(enemyEntity);
                
                AddComponent(enemyEntity, new RandomComponent { Random = Random.CreateFromIndex((uint)UnityEngine.Random.Range(1, 1000000)) });
                AddComponent(enemyEntity, new AttackSpeedComponent { AttackSpeed = 1.0f / stats.AttackSpeed.Value });
                AddComponent(enemyEntity, new MoneyOnDeathComponent { Amount = authoring.enemyData.MoneyOnDeath});
                AddComponent(enemyEntity, new SimpleDamageComponent { Damage = stats.HealthDamage.Value, });
                AddComponent(enemyEntity, new SpeedComponent { Speed = stats.MovementSpeed.Value });
                AddComponent(enemyEntity, new FresnelComponent { Value = 5f });
                
                AddComponent(enemyEntity, new HealthComponent
                {
                    Bar = GetEntity(authoring.healthbar, TransformUsageFlags.Dynamic),
                    Health = stats.MaxHealth.Value,
                    Armor = stats.MaxArmor.Value,
                    Shield = stats.MaxShield.Value,
                });
                
                AddComponent(enemyEntity, new MaxHealthComponent
                {
                    Health = stats.MaxHealth.Value,
                    Armor = stats.MaxArmor.Value,
                    Shield = stats.MaxShield.Value,
                });
                
                AddComponent(enemyEntity, new HealthScalingComponent
                {
                    Multiplier = authoring.EnemyData.HealthScalingMultiplier
                });

                if (authoring.enemyData.ExplodeOnDeath)
                {
                    AddComponent(enemyEntity, new ExplosionOnDeathComponent { Size = authoring.enemyData.ExplosionSize });
                }

                if (authoring.enemyData.CanDropLoot)
                {
                    AddComponent(enemyEntity, new LootOnDeathComponent { Probability = authoring.enemyData.DropLootChance });
                }

                if (authoring.enemyData.IsBoss)
                {
                    AddComponent(enemyEntity, new EnemyBossComponent
                    {
                        Name = authoring.enemyData.BossName,
                        Offset = authoring.enemyData.VerticalNameOffset,
                    });

                }
            }
        }
    }

    public struct EnemySpawnedTag : IComponentData
    {
        
    }

    public struct ManagedClusterComponent : IComponentData
    {
        public Entity ClusterParent;
    }

    public struct EnemyBossComponent : IComponentData
    {
        public FixedString64Bytes Name;
        public float Offset;
    }
}
using Random = Unity.Mathematics.Random;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using Unity.Entities;
using Effects.ECS;
using Enemy.ECS;
using UnityEngine;
using VFX.ECS;

namespace Enemy
{
    public class Enemy : MonoBehaviour
    {
        [Title("Authoring", "Data")]
        [SerializeField]
        private EnemyData enemyData;
        
        [Title("Authoring", "Ground Collision")]
        [SerializeField]
        private LayerMask groundMask;

        [Title("Authoring", "Movement")]
        [SerializeField]
        private float turnSpeed = 2;
        
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
                
                AddComponent(enemyEntity, new RandomComponent { Random = Random.CreateFromIndex((uint)UnityEngine.Random.Range(1, 1000000)) });
                AddComponent(enemyEntity, new AttackSpeedComponent { AttackSpeed = 1.0f / stats.AttackSpeed.Value });
                AddComponent(enemyEntity, new MoneyOnDeathComponent { Amount = authoring.enemyData.MoneyOnDeath});
                AddComponent(enemyEntity, new SimpleDamageComponent { Damage = stats.HealthDamage.Value, });
                AddComponent(enemyEntity, new SpeedComponent { Speed = stats.MovementSpeed.Value });
                AddComponent(enemyEntity, new FresnelComponent { Value = 5f});

                AddComponent(enemyEntity, new FlowFieldComponent
                {
                    Up = new float3(0, 1, 0),
                    TargetUp = new float3(0, 1, 0),
                    Forward = new float3(0, 0, 1),
                    TurnSpeed = authoring.turnSpeed,
                    
                    Importance = authoring.enemyData.Importance,
                    //LayerMask = authoring.groundMask,
                });
                
                AddComponent(enemyEntity, new HealthComponent
                {
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
            }
        }
    }

    public struct EnemySpawnedTag : IComponentData
    {
        
    }
}
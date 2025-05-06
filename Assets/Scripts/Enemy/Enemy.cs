using Effects.ECS;
using Sirenix.OdinInspector;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

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
                AddComponent(enemyEntity, new SpeedComponent { Speed = stats.MovementSpeed.Value });
                AddComponent(enemyEntity, new FresnelComponent { Value = 5f});
                AddComponent(enemyEntity, new AttackSpeedComponent { AttackSpeed = 1.0f / stats.AttackSpeed.Value });

                AddComponent(enemyEntity, new FlowFieldComponent
                {
                    Up = new float3(0, 1, 0),
                    TargetUp = new float3(0, 1, 0),
                    Forward = new float3(0, 0, 1),
                    TurnSpeed = authoring.turnSpeed,
                    LayerMask = authoring.groundMask,
                });
                
                AddComponent(enemyEntity, new Effects.ECS.HealthComponent
                {
                    Health = stats.MaxHealth.Value,
                });
                
                AddComponent(enemyEntity, new DamageComponent
                {
                    Damage = stats.DamageMultiplier.Value,
                    HasLimitedHits = true,
                    LimitedHits = 5,
                    Key = -1,
                    TriggerDamageDone = false,
                });
            }
        }
    }
}
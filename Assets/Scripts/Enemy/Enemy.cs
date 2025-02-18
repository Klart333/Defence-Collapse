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
        
        public class EnemyBaker : Baker<Enemy>
        {
            public override void Bake(Enemy authoring)
            {
                if (authoring.enemyData == null)
                {
                    return;
                }
            
                Entity enemyEntity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(enemyEntity, new SpeedComponent
                {
                    Speed = authoring.enemyData.Stats.MovementSpeed.BaseValue,
                });
            
                AddComponent(enemyEntity, new FlowFieldComponent()
                {
                    Up = new float3(0, 1, 0),
                    TargetUp = new float3(0, 1, 0),
                    Forward = new float3(0, 0, 1),
                    TurnSpeed = authoring.turnSpeed,
                    LayerMask = authoring.groundMask,
                });
            }
        }
    }
}
using InputCamera.ECS;
using Unity.Entities;
using UnityEngine;
using Enemy;

namespace Health
{
    public class HealthbarAuthoring : MonoBehaviour
    {
        [SerializeField]
        private EnemyData enemyData;
        
        private class HealthbarAuthoringBaker : Baker<HealthbarAuthoring>
        {
            public override void Bake(HealthbarAuthoring authoring)
            {
                Entity bar = GetEntity(authoring, TransformUsageFlags.Dynamic);
                Stats stats = new Stats(authoring.enemyData.Stats);
                float totalHealth = stats.MaxHealth.Value + stats.MaxArmor.Value + stats.MaxShield.Value; 

                AddComponent(bar, new HealthPropertyComponent
                {
                    Value = stats.MaxHealth.Value / totalHealth,
                });
                
                AddComponent(bar, new ArmorPropertyComponent
                {
                    Value = stats.MaxArmor.Value / totalHealth,
                });
                
                AddComponent(bar, new ShieldPropertyComponent
                {
                    Value = stats.MaxShield.Value / totalHealth,
                });
                
                AddComponent<RotateTowardCameraLTWTag>(bar);
            }
        }
    }
}
using InputCamera.ECS;
using Unity.Entities;
using UnityEngine;

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
                float totalHealth = authoring.enemyData.Stats.MaxHealth.Value + authoring.enemyData.Stats.MaxArmor.Value + authoring.enemyData.Stats.MaxShield.Value; 
                AddComponent(bar, new HealthPropertyComponent
                {
                    Value = authoring.enemyData.Stats.MaxHealth.Value / totalHealth,
                });
                
                AddComponent(bar, new ArmorPropertyComponent
                {
                    Value = authoring.enemyData.Stats.MaxArmor.Value / totalHealth,
                });
                
                AddComponent(bar, new ShieldPropertyComponent
                {
                    Value = authoring.enemyData.Stats.MaxShield.Value / totalHealth,
                });
                
                AddComponent<RotateTowardCameraLTWTag>(bar);
            }
        }
    }
}
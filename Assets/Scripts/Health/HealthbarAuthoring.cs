using Effects;
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
                Stats stats = new Stats(authoring.enemyData.StatGroups);
                float maxHealth = stats.Get<MaxHealthStat>().Value;
                float maxArmor = stats.Get<MaxArmorStat>().Value;
                float totalHealth = maxHealth + maxArmor; 

                AddComponent(bar, new HealthPropertyComponent
                {
                    Value = maxHealth / totalHealth,
                });
                
                AddComponent(bar, new ArmorPropertyComponent
                {
                    Value = maxArmor / totalHealth,
                });
                
                AddComponent<RotateTowardCameraLTWTag>(bar);
            }
        }
    }
}
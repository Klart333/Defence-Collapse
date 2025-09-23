using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;
using Juice.Ecs;
using Enemy.ECS;

namespace Gameplay.Chunk.ECS
{
    public class GroundObject : MonoBehaviour
    {
        [Title("Animation", "Scaling")]
        [SerializeField]
        private float scalingDuration = 0.5f;
        
        private class GroundObjectAuthoringBaker : Baker<GroundObject>
        {
            public override void Bake(GroundObject authoring)
            {
                Entity enemyEntity = GetEntity(TransformUsageFlags.Dynamic);
                
                AddComponent<GroundObjectComponent>(enemyEntity);
                AddComponent(enemyEntity, new SpeedComponent { Speed = 1 });
                AddComponent(enemyEntity, new ScaleComponent
                {
                    Duration = authoring.scalingDuration,
                    StartScale = 0,
                    TargetScale = authoring.transform.localScale.x,
                });
            }
        }
    }
}
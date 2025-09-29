using Effects.ECS;
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
                Entity groundEntity = GetEntity(TransformUsageFlags.Dynamic);
                
                AddComponent<GroundObjectComponent>(groundEntity);
                AddComponent<RandomComponent>(groundEntity);
                
                AddComponent(groundEntity, new SpeedComponent { Speed = 1 });
                AddComponent(groundEntity, new ScaleComponent
                {
                    Duration = authoring.scalingDuration,
                    StartScale = 0,
                    TargetScale = authoring.transform.localScale.x,
                });
                
            }
        }
    }
}
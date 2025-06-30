using Unity.Mathematics;
using Unity.Entities;
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
                
                AddComponent(prefab, new FlowFieldComponent
                {
                    Up = new float3(0, 1, 0),
                    TargetUp = new float3(0, 1, 0),
                    Forward = new float3(0, 0, 1),
                    TurnSpeed = 5,
                    
                    Importance = 1,
                    //LayerMask = authoring.groundMask,
                });
                
                AddComponent(prefab, new SpeedComponent
                {
                    Speed = authoring.stats.MovementSpeed.Value,
                });
            }
        }
    }

    public struct LittleDudePrefabComponent : IComponentData
    {
        public Entity Prefab;
    } 
}
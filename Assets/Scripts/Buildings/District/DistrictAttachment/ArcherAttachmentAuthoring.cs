using Buildings.District.ECS;
using Sirenix.OdinInspector;
using Unity.Transforms;
using Unity.Entities;
using Effects.ECS;
using UnityEngine;
using Enemy.ECS;

namespace Buildings.District.DistrictAttachment
{
    public class ArcherAttachmentAuthoring : MonoBehaviour, IAuthoringComponent
    {
        [Title("References")]
        [SerializeField]
        private GameObject lowerString;
        
        [SerializeField]
        private GameObject upperString;

        [SerializeField]
        private float stringLength = 0.8f;

        [SerializeField]
        private float lengthAtFull = 0.8f;
        
        public MonoBehaviour AuthoringComponent => this;
        
        private class ArcherAttachmentAuthoringBaker : Baker<ArcherAttachmentAuthoring>
        {
            public override void Bake(ArcherAttachmentAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                
                AddComponent<AttachementMeshComponent>(entity);
                AddComponent<SmoothMovementComponent>(entity);
                AddComponent<AttachmentAttackValue>(entity);
                AddComponent<LocalTransform>(entity);
                AddComponent<LocalToWorld>(entity);
                
                AddComponent(entity, new SpeedComponent { Speed = 1});
                
                AddComponent(entity, new AnimateBowComponent
                {
                    LowerString = GetEntity(authoring.lowerString, TransformUsageFlags.Dynamic),
                    UpperString = GetEntity(authoring.upperString, TransformUsageFlags.Dynamic),
                    StringLength = authoring.stringLength * authoring.transform.localScale.x,
                    LengthAtFull = authoring.lengthAtFull * authoring.transform.localScale.x,
                });
            }
        }
    }

    public struct AnimateBowComponent : IComponentData
    {
        public Entity LowerString;
        public Entity UpperString;
        public float StringLength;
        public float LengthAtFull;
    }
}
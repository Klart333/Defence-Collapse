using Unity.Entities;
using UnityEngine;

namespace Buildings.District.DistrictAttachment
{
    public class DistrictAttachmentAuthoring : MonoBehaviour
    {
        [SerializeField]
        private DistrictAttachmentUtility districtAttachmentUtility;
        
        private class DistrictAttachmentAuthoringBaker : Baker<DistrictAttachmentAuthoring>
        {
            public override void Bake(DistrictAttachmentAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                DynamicBuffer<DistrictAttachmentElement> buffer = AddBuffer<DistrictAttachmentElement>(entity);
                for (int i = 0; i < authoring.districtAttachmentUtility.DistrictAttachments.Length; i++)
                {
                    buffer.Add(new DistrictAttachmentElement
                    {
                        DistrictAttachment = GetEntity(authoring.districtAttachmentUtility.DistrictAttachments[i].AuthoringComponent, TransformUsageFlags.Dynamic)
                    });
                }

                AddComponent<DistrictAttachmentDatabaseTag>(entity);
            }
        }
    }
    
    public struct DistrictAttachmentElement : IBufferElementData
    {
        public Entity DistrictAttachment;
    }

    public struct DistrictAttachmentDatabaseTag : IComponentData { }
}
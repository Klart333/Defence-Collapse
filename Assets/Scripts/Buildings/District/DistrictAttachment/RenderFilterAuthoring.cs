using Unity.Entities;
using Unity.Entities.Graphics;
using UnityEngine;

namespace Buildings.District.DistrictAttachment
{
    public class RenderFilterAuthoring : MonoBehaviour
    {
        private class RenderFilterAuthoringBaker : Baker<RenderFilterAuthoring>
        {
            public override void Bake(RenderFilterAuthoring authoring)
            {
                Entity entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
                AddComponent<RenderFilterSettings>(entity);
            }
        }
    }
}
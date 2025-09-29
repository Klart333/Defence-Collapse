using Unity.Entities;
using UnityEngine;

namespace Gameplay.Chunk
{
    public class GroundObjectsAuthoring : MonoBehaviour
    {
        public GroundObjectsUtility GroundObjectsUtility;
        
        private class GroundObjectsAuthoringBaker : Baker<GroundObjectsAuthoring>
        {
            public override void Bake(GroundObjectsAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                DynamicBuffer<GroundObjectElement> buffer = AddBuffer<GroundObjectElement>(entity);
                for (int i = 0; i < authoring.GroundObjectsUtility.GroundObjects.Count; i++)
                {
                    buffer.Add(new GroundObjectElement
                    {
                        GroundObjectEntity = GetEntity(authoring.GroundObjectsUtility.GroundObjects[i], TransformUsageFlags.Dynamic)
                    });
                }

                AddComponent<GroundObjectDatabaseTag>(entity);
            }
        }
    }
    
    public struct GroundObjectElement : IBufferElementData
    {
        public Entity GroundObjectEntity;
    }

    public struct GroundObjectDatabaseTag : IComponentData { }
}
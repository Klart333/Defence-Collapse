using Unity.Entities;
using UnityEngine;
using Enemy;

namespace DataStructures.Queue.ECS
{
    public class EnemiesAuthoring : MonoBehaviour
    {
        public EnemyUtility EnemyUtility;
        
        private class EnemiesAuthoringBaker : Baker<EnemiesAuthoring>
        {
            public override void Bake(EnemiesAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                DynamicBuffer<ItemBufferElement> buffer = AddBuffer<ItemBufferElement>(entity);
                for (int i = 0; i < authoring.EnemyUtility.Enemies.Count; i++)
                {
                    buffer.Add(new ItemBufferElement
                    {
                        EnemyEntity = GetEntity(authoring.EnemyUtility.Enemies[i], TransformUsageFlags.Dynamic)
                    });
                }

                AddComponent<EnemyDatabaseTag>(entity);
            }
        }
    }

    [InternalBufferCapacity(3)]
    public struct ItemBufferElement: IBufferElementData
    {
        public Entity EnemyEntity;
    }

    public struct EnemyDatabaseTag : IComponentData
    {
    }
}
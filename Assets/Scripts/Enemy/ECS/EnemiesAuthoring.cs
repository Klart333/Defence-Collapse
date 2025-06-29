using Unity.Entities;
using UnityEngine;
using Enemy;

namespace Enemy.ECS
{
    public class EnemiesAuthoring : MonoBehaviour
    {
        public EnemyUtility EnemyUtility;
        
        private class EnemiesAuthoringBaker : Baker<EnemiesAuthoring>
        {
            public override void Bake(EnemiesAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                DynamicBuffer<EnemyBufferElement> buffer = AddBuffer<EnemyBufferElement>(entity);
                for (int i = 0; i < authoring.EnemyUtility.Enemies.Count; i++)
                {
                    buffer.Add(new EnemyBufferElement
                    {
                        EnemyEntity = GetEntity(authoring.EnemyUtility.Enemies[i], TransformUsageFlags.Dynamic)
                    });
                }

                AddComponent<EnemyDatabaseTag>(entity);
            }
        }
    }

    [InternalBufferCapacity(4)]
    public struct EnemyBufferElement: IBufferElementData
    {
        public Entity EnemyEntity;
    }

    public struct EnemyDatabaseTag : IComponentData
    {
    }
}
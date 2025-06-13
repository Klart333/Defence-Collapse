using Unity.Entities;
using UnityEngine;

namespace Enemy.ECS
{
    public class EnemyBossAuthoring : MonoBehaviour
    {
        public EnemyUtility EnemyUtility;
        
        private class EnemyBossAuthoringBaker : Baker<EnemyBossAuthoring>
        {
            public override void Bake(EnemyBossAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                DynamicBuffer<EnemyBossElement> buffer = AddBuffer<EnemyBossElement>(entity);
                for (int i = 0; i < authoring.EnemyUtility.Enemies.Count; i++)
                {
                    buffer.Add(new EnemyBossElement
                    {
                        EnemyEntity = GetEntity(authoring.EnemyUtility.Enemies[i], TransformUsageFlags.Dynamic)
                    });
                }

                AddComponent<EnemyBossDatabaseTag>(entity);
            }
        }
    }
    
    [InternalBufferCapacity(1)]
    public struct EnemyBossElement: IBufferElementData
    {
        public Entity EnemyEntity;
    }

    public struct EnemyBossDatabaseTag : IComponentData
    {
    }
}
using Unity.Entities;
using UnityEngine;

namespace Effects.ECS
{
    public class MoneyPrefabAuthoring : MonoBehaviour
    {
        [SerializeField]
        private MoneyEntityAuthoring moneyPrefab;
        
        public MoneyEntityAuthoring MoneyPrefab => moneyPrefab;
        
        private class MoneyPrefabAuthoringBaker : Baker<MoneyPrefabAuthoring>
        {
            public override void Bake(MoneyPrefabAuthoring authoring)
            {
                Entity moneyPrefab = GetEntity(authoring.MoneyPrefab, TransformUsageFlags.Dynamic);
                
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new MoneyPrefabComponent { MoneyPrefab = moneyPrefab });
            }
        }
    }

    public struct MoneyPrefabComponent : IComponentData
    {
        public Entity MoneyPrefab;
    }
}
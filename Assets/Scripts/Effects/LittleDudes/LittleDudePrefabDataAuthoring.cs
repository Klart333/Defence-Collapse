using Unity.Entities;
using UnityEngine;

namespace Effects.LittleDudes
{
    public class LittleDudePrefabDataAuthoring : MonoBehaviour
    {
        [SerializeField]
        private GameObject littleDudePrefab; 

        private class LittleDudePrefabDataAuthoringBaker : Baker<LittleDudePrefabDataAuthoring>
        {
            public override void Bake(LittleDudePrefabDataAuthoring authoring)
            {
                Entity prefab = GetEntity(authoring.littleDudePrefab, TransformUsageFlags.Dynamic);
                
                Entity entity = GetEntity(TransformUsageFlags.None);
                
                AddComponent(entity, new LittleDudePrefabComponent
                {
                    Prefab = prefab,
                });
            }
        }
    }
}
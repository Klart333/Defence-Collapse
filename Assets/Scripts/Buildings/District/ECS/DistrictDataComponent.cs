using Unity.Mathematics;
using Unity.Entities;

namespace Buildings.District.ECS
{
    public struct DistrictDataComponent : IComponentData
    {
        public int DistrictID;
    }

    public struct AttachementMeshComponent : IComponentData
    {
        public Entity Target;
    }

    public struct TargetingActivationComponent : IComponentData
    {
        public int Count;
    }
    
    public struct UpdateTargetingTag : IComponentData { }
    
    public struct DistrictEntityDataComponent : IComponentData
    {
        public float3 OriginPosition;
        public float3 TargetPosition;
        public int DistrictID;
    }
}
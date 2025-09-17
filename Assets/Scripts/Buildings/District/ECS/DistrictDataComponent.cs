using Unity.Entities;
using Unity.Mathematics;

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
    
    public struct DistrictEntityData : IComponentData
    {
        public float3 OriginPosition;
        public float3 TargetPosition;
        public int DistrictID;
    }
}
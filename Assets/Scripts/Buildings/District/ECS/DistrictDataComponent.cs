using Unity.Entities;
using Unity.Mathematics;

namespace Buildings.District.ECS
{
    public struct DistrictDataComponent : IComponentData
    {
        public int DistrictID;
    }

    public struct TargetMeshComponent : IComponentData
    {
        public Entity Target;
    }
    
    public struct DistrictEntityData : IComponentData
    {
        public int DistrictID;
        public float3 TargetPosition;
        public float3 OriginPosition;
    }
}
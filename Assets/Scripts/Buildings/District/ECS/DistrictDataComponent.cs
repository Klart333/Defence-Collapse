using System;
using Unity.Burst;
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
    
    //[BurstCompile]
    public struct DistrictEntityDataComponent : IComponentData/*, IEquatable<DistrictEntityDataComponent>*/
    {
        public float3 OriginPosition;
        public float3 TargetPosition;
        public int DistrictID;

        /*
        [BurstCompile]
        public bool Equals(DistrictEntityDataComponent other)
        {
            return DistrictID == other.DistrictID;
        }

        [BurstCompile]
        public override bool Equals(object obj)
        {
            return obj is DistrictEntityDataComponent other && Equals(other);
        }

        [BurstCompile]
        public override int GetHashCode()
        {
            return DistrictID.GetHashCode();
        }

        [BurstCompile]
        public static bool operator ==(DistrictEntityDataComponent left, DistrictEntityDataComponent right)
        {
            return left.Equals(right);
        }

        [BurstCompile]
        public static bool operator !=(DistrictEntityDataComponent left, DistrictEntityDataComponent right)
        {
            return !left.Equals(right);
        }*/
    }
}
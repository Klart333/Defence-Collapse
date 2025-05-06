using Unity.Entities;
using Unity.Mathematics;

namespace InputCamera.ECS
{
    public struct CameraPositionComponent : IComponentData
    {
        public float3 Position;
    }    
    
    public struct RotateTowardCameraTag : IComponentData { }
}

using Unity.Entities;
using Unity.Rendering;

namespace Health
{
    [MaterialProperty("_Health")]
    public struct HealthPropertyComponent : IComponentData
    {
        public float Value;
    }
    
    
    [MaterialProperty("_Armor")]
    public struct ArmorPropertyComponent : IComponentData
    {
        public float Value;
    }
    
    
    [MaterialProperty("_Shield")]
    public struct ShieldPropertyComponent : IComponentData
    {
        public float Value;
    }
    
    public enum HealthType : byte
    {
        Health,
        Armor,
        Shield
    }
}
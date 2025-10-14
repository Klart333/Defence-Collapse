using System;
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
    
    [Flags]
    public enum HealthType : byte
    {
        Health = 1 << 0,
        Armor = 1 << 1,
    }
}
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
// ReSharper disable Unity.BurstLoadingManagedType

namespace Effects
{
    [Serializable, InlineProperty]
    public struct StatType : IEquatable<StatType>
    {
        public StatType(Type type)
        {
            _type = type;
        }
        
        [ValueDropdown("GetStateTypes")]
        [OdinSerialize, HideLabel] 
        private Type _type;

        public Type Type => _type;
        
        // Odin dropdown to select available DistrictState subclasses
        public static ValueDropdownList<Type> GetStateTypes()
        {
            ValueDropdownList<Type> list = new ValueDropdownList<Type>();
            foreach (Type t in typeof(Stat).Assembly.GetTypes())
            {
                if (t.IsSubclassOf(typeof(Stat)) && !t.IsAbstract)
                {
                    list.Add(t.Name, t);
                }
            }
            return list;
        }

        public bool Equals(StatType other)
        {
            return _type == other._type;
        }

        public override bool Equals(object obj)
        {
            return obj is StatType other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (_type != null ? _type.GetHashCode() : 0);
        }
    }
}
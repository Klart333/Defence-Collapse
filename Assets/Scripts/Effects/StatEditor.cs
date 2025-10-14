using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;

namespace Effects
{
    [Serializable, InlineProperty]
    public struct StatTypeType : IEquatable<StatTypeType>
    {
        [ValueDropdown("GetStateTypes")]
        [OdinSerialize, HideLabel] 
        private Type statType;

        public Type StatType => statType;
        
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

        public bool Equals(StatTypeType other)
        {
            return statType == other.statType;
        }

        public override bool Equals(object obj)
        {
            return obj is StatTypeType other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (statType != null ? statType.GetHashCode() : 0);
        }

        public static implicit operator StatTypeType(Type statType)
        {
            return new StatTypeType
            {
                statType = statType,
            };
        }
    }
}
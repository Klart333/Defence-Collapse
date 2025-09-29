using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using System;

namespace Buildings.District
{
    public interface IDistrictStateCreator
    {
        public DistrictState CreateDistrictState(DistrictData districtData, TowerData towerData, Vector3 position, int key);
    }

    [Serializable]
    public class GenericDistrictStateCreator : IDistrictStateCreator
    {
        [ValueDropdown("GetDistrictStateTypes")]
        [SerializeField, OdinSerialize] 
        private Type stateType;

        public DistrictState CreateDistrictState(DistrictData districtData, TowerData towerData, Vector3 position, int key)
        {
            if (stateType == null)
            {
                Debug.LogError("State type is null!");
                return null;
            }

            return (DistrictState)Activator.CreateInstance(stateType, districtData, towerData, position, key);
        }

        // Odin dropdown to select available DistrictState subclasses
        private static ValueDropdownList<Type> GetDistrictStateTypes()
        {
            ValueDropdownList<Type> list = new ValueDropdownList<Type>();
            foreach (Type t in typeof(DistrictState).Assembly.GetTypes())
            {
                if (t.IsSubclassOf(typeof(DistrictState)) && !t.IsAbstract)
                {
                    list.Add(t.Name, t);
                }
            }
            return list;
        }
    }
}
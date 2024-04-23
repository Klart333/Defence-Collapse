using System.Collections.Generic;
using UnityEngine;

namespace Buildings
{
    public class Path : PooledMonoBehaviour, IBuildable
    {
        public void Setup(PrototypeData prototypeData, List<Material> materials)
        {
            GetComponentInChildren<MeshFilter>().mesh = prototypeData.MeshRot.Mesh;
            GetComponentInChildren<MeshRenderer>().SetMaterials(materials);
        }

        public void ToggleIsBuildableVisual(bool value)
        {
            print("Value: " + value);
        }   
    }
}

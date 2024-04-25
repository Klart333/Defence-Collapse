using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

namespace Buildings
{
    public class Path : PooledMonoBehaviour, IBuildable
    {
        [Title("Visual")]
        [SerializeField]
        private Material transparentGreen;

        private List<Material> transparentMaterials = new List<Material>();
        public List<Material> Materials { get; set; }

        public void Setup(PrototypeData prototypeData, List<Material> materials)
        {
            GetComponentInChildren<MeshFilter>().mesh = prototypeData.MeshRot.Mesh;
            GetComponentInChildren<MeshRenderer>().SetMaterials(materials);

            Materials = materials;
            transparentMaterials = new List<Material>();
            for (int i = 0; i < materials.Count; i++)
            {
                transparentMaterials.Add(transparentGreen);
            }
        }

        public void ToggleIsBuildableVisual(bool value)
        {
            MeshRenderer rend = GetComponentInChildren<MeshRenderer>();

            if (value)
            {
                rend.SetMaterials(transparentMaterials);
            }
            else
            {
                rend.SetMaterials(Materials);
            }
        }   
    }
}

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

        private MeshCollider meshCollider;
        private MeshRenderer meshRenderer;
        
        public Mesh Mesh { get; set; }

        public MeshRenderer MeshRenderer
        {
            get
            {
                if (meshRenderer == null) meshRenderer = GetComponentInChildren<MeshRenderer>();

                return meshRenderer;
            }
        }
        public MeshCollider MeshCollider
        {
            get
            {
                if (meshCollider == null) meshCollider = GetComponentInChildren<MeshCollider>();

                return meshCollider;
            }
        }

        public List<Material> Materials { get; set; }

        public void Setup(PrototypeData prototypeData, List<Material> materials)
        {
            Mesh = prototypeData.MeshRot.Mesh;

            GetComponentInChildren<MeshFilter>().mesh = prototypeData.MeshRot.Mesh;
            MeshRenderer.SetMaterials(materials);

            Materials = materials;
            transparentMaterials = new List<Material>();
            for (int i = 0; i < materials.Count; i++)
            {
                transparentMaterials.Add(transparentGreen);
            }
        }

        public void ToggleIsBuildableVisual(bool value)
        {
            if (value)
            {
                MeshRenderer.SetMaterials(transparentMaterials);
            }
            else
            {
                MeshCollider.sharedMesh = Mesh;
                MeshRenderer.SetMaterials(Materials);
            }
        }   
    }
}

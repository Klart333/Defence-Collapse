using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Buildings
{
    public class Path : PooledMonoBehaviour, IBuildable
    {
        [Title("Visual")]
        [SerializeField]
        private MaterialData materialData;

        [SerializeField]
        private Material transparentGreen;

        private List<Material> transparentMaterials = new List<Material>();

        private MeshCollider meshCollider;
        private MeshRenderer meshRenderer;
        
        public PrototypeData PrototypeData { get; private set; }
        public int Importance => 0;

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

        protected override void OnDisable()
        {
            base.OnDisable();

            Reset();
        }

        private void Reset()
        {
            transform.localScale = Vector3.one;
            MeshRenderer.transform.localScale = Vector3.one;
        }

        public void Setup(PrototypeData prototypeData, Vector3 scale)
        {
            PrototypeData = prototypeData;

            GetComponentInChildren<MeshFilter>().mesh = prototypeData.MeshRot.Mesh;
            MeshRenderer.SetMaterials(materialData.GetMaterials(PrototypeData.MaterialIndexes));

            transparentMaterials = new List<Material>();
            for (int i = 0; i < PrototypeData.MaterialIndexes.Length; i++)
            {
                transparentMaterials.Add(transparentGreen);
            }

            MeshRenderer.transform.localScale = scale;
        }

        public void ToggleIsBuildableVisual(bool value)
        {
            if (value)
            {
                MeshRenderer.SetMaterials(transparentMaterials);
            }
            else
            {
                MeshCollider.sharedMesh = PrototypeData.MeshRot.Mesh;
                MeshRenderer.SetMaterials(materialData.GetMaterials(PrototypeData.MaterialIndexes));
            }
        }   
    }
}

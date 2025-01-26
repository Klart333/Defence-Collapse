using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Buildings
{
    public class Path : PooledMonoBehaviour, IBuildable
    {
        [Title("Visual")]
        [SerializeField]
        private MaterialData materialData;

        [SerializeField]
        private Material transparentGreen;

        [Title("Events")]
        [SerializeField]
        private UnityEvent OnPlacedEvent;

        [SerializeField]
        private UnityEvent OnResetEvent;

        private List<Material> transparentMaterials = new List<Material>();

        private MeshCollider meshCollider;
        private MeshRenderer meshRenderer;
        
        public PrototypeData PrototypeData { get; private set; }
        public int Importance => 0;

        public MeshRenderer MeshRenderer => meshRenderer ??= GetComponentInChildren<MeshRenderer>();
        public MeshCollider MeshCollider => meshCollider ??= GetComponentInChildren<MeshCollider>();
        public MeshWithRotation MeshRot => PrototypeData.MeshRot;

        protected override void OnDisable()
        {
            base.OnDisable();

            Reset();
        }

        private void Reset()
        {
            transform.localScale = Vector3.one;
            MeshRenderer.transform.localScale = Vector3.one;
            OnResetEvent?.Invoke();
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

                OnPlacedEvent?.Invoke();
            }
        }   
    }
}

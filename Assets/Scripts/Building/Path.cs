using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using WaveFunctionCollapse;

namespace Buildings
{
    public class Path : PooledMonoBehaviour, IBuildable
    {
        [Title("Visual")]
        [SerializeField]
        private MaterialData materialData;

        [SerializeField]
        private ProtoypeMeshes protoypeMeshes;
        
        [SerializeField]
        private Material transparentGreen;
        
        [SerializeField]
        private Material transparentRed;
        
        [SerializeField]
        private Transform meshTransform;

        [Title("Events")]
        [SerializeField]
        private UnityEvent OnPlacedEvent;

        [SerializeField]
        private UnityEvent OnResetEvent;

        private List<Material> transparentMaterials = new List<Material>();
        private List<Material> transparentRemoveMaterials = new List<Material>();

        private MeshCollider meshCollider;
        private MeshRenderer meshRenderer;
        
        public PrototypeData PrototypeData { get; private set; }
        public ChunkIndex ChunkIndex { get; private set; }
        public int Importance => 0;

        public MeshRenderer MeshRenderer => meshRenderer ??= GetComponentInChildren<MeshRenderer>();
        public MeshCollider MeshCollider => meshCollider ??= GetComponentInChildren<MeshCollider>();
        public MeshWithRotation MeshRot => PrototypeData.MeshRot;
        public Transform MeshTransform => meshTransform;

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

        public void Setup(PrototypeData prototypeData, ChunkIndex index, Vector3 scale)
        {
            ChunkIndex = index;
            PrototypeData = prototypeData;

            GetComponentInChildren<MeshFilter>().mesh = protoypeMeshes[prototypeData.MeshRot.MeshIndex];
            MeshRenderer.SetMaterials(materialData.GetMaterials(PrototypeData.MaterialIndexes));

            transparentMaterials.Clear();
            transparentRemoveMaterials.Clear();
            for (int i = 0; i < PrototypeData.MaterialIndexes.Length; i++)
            {
                transparentMaterials.Add(transparentGreen);
                transparentRemoveMaterials.Add(transparentRed);
            }

            MeshRenderer.transform.localScale = scale;
        }

        public void ToggleIsBuildableVisual(bool value, bool showRemoving)
        {
            if (value)
            {
                MeshRenderer.SetMaterials(showRemoving ? transparentRemoveMaterials : transparentMaterials);
            }
            else
            {
                MeshCollider.sharedMesh = protoypeMeshes[PrototypeData.MeshRot.MeshIndex];
                MeshRenderer.SetMaterials(materialData.GetMaterials(PrototypeData.MaterialIndexes));

                OnPlacedEvent?.Invoke();
            }
        }   
    }
}

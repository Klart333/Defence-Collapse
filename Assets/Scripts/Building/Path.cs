using System.Collections.Generic;
using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using UnityEngine.Events;
using UnityEngine;

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

        [Title("Colliders")]
        [SerializeField]
        private Collider[] cornerColliders;

        [SerializeField]
        private BuildableCornerData buildableCornerData;
        
        [Title("Events")]
        [SerializeField]
        private UnityEvent OnPlacedEvent;

        [SerializeField]
        private UnityEvent OnResetEvent;

        private readonly List<Material> transparentMaterials = new List<Material>();
        private readonly List<Material> transparentRemoveMaterials = new List<Material>();

        private MeshRenderer meshRenderer;
        
        public PrototypeData PrototypeData { get; private set; }
        public ChunkIndex ChunkIndex { get; private set; }

        public MeshRenderer MeshRenderer => meshRenderer ??= GetComponentInChildren<MeshRenderer>();
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
            
            for (int i = 0; i < cornerColliders.Length; i++)
            {
                cornerColliders[i].gameObject.SetActive(false);
            }
            
            OnResetEvent?.Invoke();
        }

        public void Setup(PrototypeData prototypeData, ChunkIndex index, Vector3 scale)
        {
            ChunkIndex = index;
            PrototypeData = prototypeData;
            transform.localScale = scale;

            GetComponentInChildren<MeshFilter>().mesh = protoypeMeshes[prototypeData.MeshRot.MeshIndex];
            MeshRenderer.SetMaterials(materialData.GetMaterials(PrototypeData.MaterialIndexes));

            transparentMaterials.Clear();
            transparentRemoveMaterials.Clear();
            for (int i = 0; i < PrototypeData.MaterialIndexes.Length; i++)
            {
                transparentMaterials.Add(transparentGreen);
                transparentRemoveMaterials.Add(transparentRed);
            }
        }

        public void ToggleIsBuildableVisual(bool value, bool showRemoving)
        {
            if (value)
            {
                MeshRenderer.SetMaterials(showRemoving ? transparentRemoveMaterials : transparentMaterials);
            }
            else
            {
                MeshRenderer.SetMaterials(materialData.GetMaterials(PrototypeData.MaterialIndexes));

                SetColliders();
                OnPlacedEvent?.Invoke();
            }
        }

        private void SetColliders()
        {
            for (int i = 0; i < cornerColliders.Length; i++)
            {
                if (MeshRot.MeshIndex != -1 && buildableCornerData.BuildableDictionary.TryGetValue(protoypeMeshes[MeshRot.MeshIndex], out BuildableCorners cornerData))
                {
                    bool value = cornerData.CornerDictionary[BuildableCornerData.VectorToCorner(DirectionUtility.BuildableCorners[i].x, DirectionUtility.BuildableCorners[i].y)].Buildable;
                    cornerColliders[i].gameObject.SetActive(value);
                }
                else
                {
                    cornerColliders[i].gameObject.SetActive(false);
                }
            }
        }
    }
}

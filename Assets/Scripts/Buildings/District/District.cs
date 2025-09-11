using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using WaveFunctionCollapse;

namespace Buildings.District
{
    public class District : PooledMonoBehaviour, IBuildable
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
        
        [SerializeField]
        private LayerMask highlightedLayer;

        [SerializeField]
        private LayerMask defaultLayer;
        
        private readonly List<Material> transparentMaterials = new List<Material>();
        private readonly List<Material> transparentRemoveMaterials = new List<Material>();
        
        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;
        private DistrictHandler districtHandler;
        
        public ChunkIndex ChunkIndex { get; private set; }
        public PrototypeData Prototype { get; private set; }
        public MeshWithRotation MeshRot { get; private set; }
        public Transform MeshTransform => meshTransform;
        public MeshRenderer MeshRenderer => meshRenderer ??= GetComponentInChildren<MeshRenderer>();
        public MeshFilter MeshFilter => meshFilter ??= GetComponentInChildren<MeshFilter>();

        private void Awake()
        {
            districtHandler = FindFirstObjectByType<DistrictHandler>();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            
            districtHandler?.RemoveDistrictObject(this);
            MeshRenderer.gameObject.layer = (int)Mathf.Log(defaultLayer.value, 2);
        }

        public void Setup(PrototypeData prototypeData, ChunkIndex chunkIndex, Vector3 scale)
        {
            ChunkIndex = chunkIndex;
            Prototype = prototypeData;
            transform.localScale = scale;

            if (Prototype.MeshRot.MeshIndex == -1)
            {
                MeshFilter.sharedMesh = null;
                return;
            }

            if (Prototype.MaterialIndexes == null)
            {
                Debug.LogError("I'll just go kill myself");
                return;
            }

            MeshFilter.sharedMesh = protoypeMeshes.Meshes[Prototype.MeshRot.MeshIndex];
            MeshRenderer.SetMaterials(materialData.GetMaterials(Prototype.MaterialIndexes));

            transparentMaterials.Clear();
            transparentRemoveMaterials.Clear();
            for (int i = 0; i < Prototype.MaterialIndexes.Length; i++)
            {
                transparentMaterials.Add(transparentGreen);
                transparentRemoveMaterials.Add(transparentRed);
            }
        }

        public void ToggleIsBuildableVisual(bool isQueried, bool showRemoving)
        {
            if (isQueried)
            {
                MeshRenderer.SetMaterials(showRemoving ? transparentRemoveMaterials : transparentMaterials);
            }
            else
            {
                if (Prototype.MaterialIndexes != null)
                {
                    MeshRenderer.SetMaterials(materialData.GetMaterials(Prototype.MaterialIndexes));
                }

                Place();
            }
        }

        private void Place()
        {
            districtHandler.AddDistrictObject(this);
        }

        public void Highlight(bool isHover)
        {
            if (isHover)
            {
                MeshRenderer.gameObject.layer = (int)Mathf.Log(highlightedLayer.value, 2);
            }
            else
            {
                MeshRenderer.gameObject.layer = (int)Mathf.Log(defaultLayer.value, 2);
            }
        }
    }
}
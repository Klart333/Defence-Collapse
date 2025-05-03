using System.Collections.Generic;
using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using UnityEngine;

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

        private readonly List<Material> transparentMaterials = new List<Material>();
        private readonly List<Material> transparentRemoveMaterials = new List<Material>();
        
        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;

        public PrototypeData Prototype { get; private set; }
        public MeshWithRotation MeshRot { get; private set; }
        public Transform MeshTransform => meshTransform;
        public ChunkIndex ChunkIndex { get; private set; }
        public MeshRenderer MeshRenderer => meshRenderer ??= GetComponentInChildren<MeshRenderer>();
        public MeshFilter MeshFilter => meshFilter ??= GetComponentInChildren<MeshFilter>();
        
        public void Setup(PrototypeData prototypeData, Vector3 scale)
        {
            ChunkIndex? nullableIndex = BuildingManager.Instance.GetIndex(transform.position + scale / 2.0f);
            if (!nullableIndex.HasValue)
            {
                Debug.LogError("Could not find chunk index");
                return;
            }
        
            ChunkIndex = nullableIndex.Value;
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
            }
        }
    }
}
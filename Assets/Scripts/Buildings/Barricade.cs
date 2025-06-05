using System.Collections.Generic;
using DataStructures.Queue.ECS;
using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using UnityEngine.Events;
using Pathfinding;
using UnityEngine;

namespace Buildings
{
    public class Barricade : PooledMonoBehaviour, IBuildable
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
        private Indexer indexer;
        
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
        private BarricadeHandler barricadeHandler;
        
        public PrototypeData PrototypeData { get; private set; }
        public ChunkIndex ChunkIndex { get; private set; }

        public BarricadeHandler BarricadeHandler => barricadeHandler ??= FindAnyObjectByType<BarricadeHandler>();
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

            BarricadeHandler?.RemoveBarricade(this);
            
            OnResetEvent?.Invoke();
        }

        public void Setup(PrototypeData prototypeData, ChunkIndex index, Vector3 scale)
        {
            if (prototypeData.MeshRot.MeshIndex == -1) return;
            
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

                Place();
            }
        }

        private void Place()
        { 
            SetColliders();

            indexer.OnRebuilt += IndexerOnOnRebuilt;
            indexer.NeedsRebuilding = true;
            indexer.DelayFrames = 1;

            BarricadeHandler.AddBarricade(this);
            OnPlacedEvent?.Invoke();
        }

        private void IndexerOnOnRebuilt()
        {
            indexer.OnRebuilt -= IndexerOnOnRebuilt;
        
            ChunkIndex chunkIndex = ChunkIndex;
            for (int i = 0; i < indexer.Indexes.Count; i++)
            {
                PathIndex index = indexer.Indexes[i];
                AttackingSystem.DamageEvent.TryAdd(index, x => BarricadeHandler.BarricadeTakeDamage(chunkIndex, x, index));
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

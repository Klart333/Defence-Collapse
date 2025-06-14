﻿using System.Collections.Generic;
using Buildings;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using UnityEngine.Events;
using Pathfinding;
using UnityEngine;
using Enemy.ECS;

namespace Buildings
{
    public class Building : PooledMonoBehaviour, IBuildable
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
        private LayerMask highlightedLayer;

        [SerializeField]
        private LayerMask selectedLayer;

        [SerializeField]
        private Transform meshTransform;

        [Title("Collider")]
        [SerializeField]
        private Hovered[] cornerColliders;

        [SerializeField]
        private Indexer indexer;

        [SerializeField]
        private BuildableCornerData buildableCornerData;

        [Title("Events")]
        [SerializeField]
        private UnityEvent OnPlacedEvent;

        [SerializeField]
        private UnityEvent OnResetEvent;

        private readonly List<Material> transparentRemoveMaterials = new List<Material>();
        private readonly List<Material> transparentMaterials = new List<Material>();

        private BuildingAnimator buildingAnimator;
        private BuildingHandler buildingHandler;
        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;
        private PathTarget pathTarget;

        private int originalLayer;
        private bool highlighted;
        private bool selected;

        public PrototypeData Prototype { get; private set; }
        public ChunkIndex ChunkIndex { get; private set; }
        public int BuildingGroupIndex { get; set; } = -1;

        private BuildingAnimator BuildingAnimator => buildingAnimator ??= FindAnyObjectByType<BuildingAnimator>();
        public BuildingHandler BuildingHandler => buildingHandler ??= FindAnyObjectByType<BuildingHandler>();
        public MeshRenderer MeshRenderer => meshRenderer ??= GetComponentInChildren<MeshRenderer>();
        public MeshFilter MeshFilter => meshFilter ??= GetComponentInChildren<MeshFilter>();
        public MeshWithRotation MeshRot => Prototype.MeshRot;
        public Transform MeshTransform => meshTransform;
        public PathTarget PathTarget => pathTarget;

        private void Awake()
        {
            originalLayer = gameObject.layer;
            pathTarget = GetComponent<PathTarget>();
        }

        private void OnEnable()
        {
            Events.OnBuildingClicked += OnBuildingClicked;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            ResetState();

            Events.OnBuildingClicked -= OnBuildingClicked;
        }

        private void ResetState()
        {
            transform.localScale = Vector3.one;
            MeshRenderer.transform.localScale = Vector3.one;

            BuildingHandler?.RemoveBuilding(this);
            BuildingGroupIndex = -1;

            selected = false;
            MeshRenderer.gameObject.layer = originalLayer;

            for (int i = 0; i < cornerColliders.Length; i++)
            {
                cornerColliders[i].gameObject.SetActive(false);
            }

            OnResetEvent?.Invoke();
        }

        #region Highlight

        public async UniTaskVoid Highlight()
        {
            if (highlighted) return;

            BuildingAnimator.BounceInOut(meshTransform);
            MeshRenderer.gameObject.layer = (int)Mathf.Log(highlightedLayer.value, 2);

            await UniTask.NextFrame();
            highlighted = true;
        }

        public void Lowlight()
        {
            MeshRenderer.gameObject.layer = originalLayer;
            highlighted = false;
        }

        public void OnSelected()
        {
            if (selected) return;

            if (highlighted)
            {
                BuildingAnimator.BounceInOut(meshTransform);
            }

            MeshRenderer.gameObject.layer = (int)Mathf.Log(selectedLayer.value, 2);

            //buildingHandler[this].State.OnSelected(transform.position);
            selected = true;
        }

        public void OnDeselected()
        {
            if (!selected) return;

            MeshRenderer.gameObject.layer = (int)Mathf.Log(highlightedLayer.value, 2); // sure ?

            //buildingHandler.BuildingData[Index].State.OnDeselected();
            selected = false;
        }

        #endregion

        private void Update()
        {
            if (!InputManager.Instance.Fire.WasReleasedThisFrame())
            {
                return;
            }

            bool hovered = false;
            for (int i = 0; i < cornerColliders.Length; i++)
            {
                if (!cornerColliders[i].IsHovered) continue;

                hovered = true;
                break;
            }

            if (hovered && InputManager.Instance.GetShift)
            {
                BuildingHandler.HighlightGroup(this);
            }

            if (highlighted && !hovered)
            {
                BuildingHandler.LowlightGroup(this);
            }
        }

        public void Setup(PrototypeData prototypeData, ChunkIndex index, Vector3 scale)
        {
            ChunkIndex = index;
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
            for (int i = 0; i < Prototype.MaterialIndexes.Length - 1; i++)
            {
                transparentMaterials.Add(transparentGreen);
                transparentRemoveMaterials.Add(transparentRed);
            }
        }

        private void OnBuildingClicked(BuildingType arg0)
        {
            BuildingHandler.LowlightGroup(this);
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
            SetColliders();

            indexer.OnRebuilt += IndexerOnOnRebuilt;
            indexer.NeedsRebuilding = true;
            indexer.DelayFrames = 1;

            BuildingHandler.AddBuilding(this);
            OnPlacedEvent?.Invoke();
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

        private void IndexerOnOnRebuilt()
        {
            indexer.OnRebuilt -= IndexerOnOnRebuilt;

            ChunkIndex chunkIndex = ChunkIndex;
            for (int i = 0; i < indexer.Indexes.Count; i++)
            {
                PathIndex index = indexer.Indexes[i];
                AttackingSystem.DamageEvent.TryAdd(index, x => BuildingHandler.BuildingTakeDamage(chunkIndex, x, index));
            }
        }

        public void OnDestroyed()
        {
            for (int i = 0; i < indexer.Indexes.Count; i++)
            {
                StopAttackingSystem.KilledIndexes.Enqueue(indexer.Indexes[i]);
            }

            for (int i = 0; i < indexer.Indexes.Count; i++)
            {
                PathIndex index = indexer.Indexes[i];
                AttackingSystem.DamageEvent.Remove(index);
            }

            gameObject.SetActive(false);
        }
    }

}
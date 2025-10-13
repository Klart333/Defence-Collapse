using System.Collections.Generic;
using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using UnityEngine.Events;
using Pathfinding;
using UnityEngine;
using System;

namespace Buildings
{
    public class Barricade : PooledMonoBehaviour, IPathTarget
    {
        public event Action OnIndexerRebuild;

        [Title("Visual")]
        [SerializeField]
        private MaterialData materialData;

        [SerializeField]
        private ProtoypeMeshes protoypeMeshes;
        
        [SerializeField]
        private Transform meshTransform;
        
        [Title("Events")]
        [SerializeField]
        private UnityEvent OnPlacedEvent;

        [SerializeField]
        private UnityEvent OnResetEvent;
        
        private MeshRenderer meshRenderer;

        public ChunkIndexEdge ChunkIndexEdge { get; private set; }
        public List<PathIndex> TargetIndexes { get; private set; }

        public MeshRenderer MeshRenderer => meshRenderer ??= GetComponentInChildren<MeshRenderer>();
        public byte Importance => 1;

        protected override void OnDisable()
        {
            base.OnDisable();

            Reset();
        }

        private void Reset()
        {
            transform.localScale = Vector3.one;
            MeshRenderer.transform.localScale = Vector3.one;

            if (TargetIndexes != null)
            {
                if (ChunkIndexEdge.EdgeType == EdgeType.North)
                {
                    PathManager.Instance.NorthEdgeBarricadeSet.Unregister(this);
                }
                else
                {
                    PathManager.Instance.WestEdgeBarricadeSet.Unregister(this);
                }

                TargetIndexes = null;
            }
            
            
            OnResetEvent?.Invoke();
        }

        public void Place(ChunkIndexEdge edge)
        { 
            ChunkIndexEdge = edge;
            
            TargetIndexes = new List<PathIndex>
            {
                new PathIndex(edge.Index.xz, edge.CellIndex.xz)
            };
            
            if (edge.EdgeType == EdgeType.North)
            {
                PathManager.Instance.NorthEdgeBarricadeSet.Register(this);
            }
            else
            {
                PathManager.Instance.WestEdgeBarricadeSet.Register(this);
            }
            
            OnPlacedEvent?.Invoke();
        }

        public void Destroyed()
        {
            gameObject.SetActive(false);
        }
    }
}

using System;
using Random = UnityEngine.Random;
using System.Collections.Generic;
using System.Linq;
using Buildings;
using Cysharp.Threading.Tasks;
using Gameplay.Money;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using WaveFunctionCollapse;

namespace Chunks
{
    public class TreeGrower : PooledMonoBehaviour
    {
        public event Action<TreeGrower> OnPlaced;
        
        [Title("Tree")]
        [SerializeField]
        private PooledMonoBehaviour[] trees;
        
        [SerializeField]
        private Material treeMaterial;

        [SerializeField]
        private Vector2 raycastArea;

        [SerializeField]
        private float treeRadius = 0.2f;

        [SerializeField]
        private int raycastCount = 100;

        [SerializeField]
        private float totalTime = 2.0f;
        
        [Title("Remove")]
        [SerializeField]
        private bool shouldRemoveWhenPlaced = true;

        [SerializeField, ShowIf(nameof(shouldRemoveWhenPlaced))]
        private float goldPerTree;

        private readonly List<PooledMonoBehaviour> spawnedTrees = new List<PooledMonoBehaviour>();
        
        private List<ChunkIndex> chunkIndexes = new List<ChunkIndex>();
        
        public bool ShouldRemoveWhenPlaced => shouldRemoveWhenPlaced;
        public int3 ChunkKey { get; set; }

        public bool HasGrown { get; set; }
        public Cell Cell { get; set; }

        private void OnEnable()
        {
            if (shouldRemoveWhenPlaced)
            {
                Events.OnBuildingBuilt += OnBuildingBuilt;   
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            
            spawnedTrees.Clear();
            HasGrown = false;
            
            chunkIndexes.Clear();
            Events.OnBuildingBuilt -= OnBuildingBuilt;   
        }

        [Button]
        public async UniTaskVoid GrowTrees()
        {
            Vector3 offset = raycastArea.ToXyZ(1).MultiplyByAxis(transform.localScale) / 2.0f;
            Vector3 min = transform.position - offset;
            Vector3 max = transform.position + offset;
            List<Vector2> positions = new List<Vector2>();
            TimeSpan delay = TimeSpan.FromSeconds(totalTime / raycastCount);
            
            for (int i = 0; i < raycastCount; i++)
            {
                Vector3 pos = new Vector3(Random.Range(min.x, max.x), 2, Random.Range(min.z, max.z));
                if (!CheckCollisionWithTrees(pos.XZ())) continue;

                Ray ray = new Ray(pos, Vector3.down);
                Material mat = GetHitMaterial(ray, out RaycastHit hit);

                if (mat != treeMaterial) continue;
                
                positions.Add(pos.XZ());
                SpawnTree(hit.point);
                await UniTask.Delay(delay);
            }

            return;
            
            bool CheckCollisionWithTrees(Vector2 pos)
            {
                for (int j = 0; j < positions.Count; j++)
                {
                    if (Vector2.Distance(positions[j], pos) < treeRadius)
                    {
                        return false;
                    }
                }   

                return true;
            }
        }
        
        private void SpawnTree(Vector3 pos)
        {
            PooledMonoBehaviour treePrefab = trees[Random.Range(0, trees.Length)];
            Quaternion rot = Quaternion.AngleAxis(Random.value * 360, Vector3.up);
            PooledMonoBehaviour spawned = treePrefab.GetAtPosAndRot<PooledMonoBehaviour>(pos, rot);
            spawnedTrees.Add(spawned);
        }

        public void ClearTrees()
        {
            for (int i = 0; i < spawnedTrees.Count; i++)
            {
                spawnedTrees[i].gameObject.SetActive(false);
            }
            
            spawnedTrees.Clear();
        }
        
        private void OnBuildingBuilt(ICollection<IBuildable> buildables)
        {
            if (chunkIndexes.Count == 0)
            {
                GetIndexes();
            }

            foreach (IBuildable buildable in buildables)
            {
                if (buildable is not Building building) continue;
                foreach (ChunkIndex chunkIndex in chunkIndexes)
                {
                    if (chunkIndex.Equals(buildable.ChunkIndex))
                    {
                        Placed();
                        return;
                    }
                }
            }

            void Placed()
            {
                foreach (PooledMonoBehaviour tree in spawnedTrees)  
                {
                    MoneyManager.Instance.AddMoneyParticles(goldPerTree, tree.transform.position);
                }
                    
                ClearTrees();
                OnPlaced?.Invoke(this);
            }
        }

        private void GetIndexes()
        {
            for (int x = 0; x < raycastArea.x; x++)
            for (int y = 0; y < raycastArea.y; y++)
            {
                Vector3 pos = transform.position + new Vector3(x, 0, y);
                ChunkIndex? index = BuildingManager.Instance.GetIndex(pos);
                if (index.HasValue)
                {
                    chunkIndexes.Add(index.Value);
                }
            }
        }

        #region Debug

        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireCube(transform.position + Vector3.up, raycastArea.ToXyZ(1).MultiplyByAxis(transform.localScale));
        }

        #endregion
        
        #region Static 
        
        private static readonly Dictionary<Collider, Renderer> ColliderToRenderers = new Dictionary<Collider, Renderer>();
        public static Material GetHitMaterial(Ray ray, out RaycastHit hit)
        {
            if (!Physics.Raycast(ray, out hit, 3, 1 << LayerMask.NameToLayer("Ground"))) 
                return null;

            if (hit.collider is not MeshCollider { sharedMesh: { } mesh }) 
                return null;

            int subMeshIndex = GetSubmeshIndex(mesh, hit.triangleIndex);
            if (subMeshIndex == -1) 
                return null;

            if (!ColliderToRenderers.TryGetValue(hit.collider, out Renderer renderer))
            {
                if (!hit.collider.TryGetComponent(out renderer))
                {
                    return null;
                }
                
                ColliderToRenderers.Add(hit.collider, renderer);
            }
            
            return subMeshIndex < renderer.sharedMaterials.Length 
                ? renderer.sharedMaterials[subMeshIndex] 
                : null;
        }

        private static readonly Dictionary<Tuple<Mesh, int>, int> SavedSubMeshIndexes = new Dictionary<Tuple<Mesh, int>, int>();
        private static int GetSubmeshIndex(Mesh mesh, int triangleIndex)
        {
            if (SavedSubMeshIndexes.TryGetValue(new Tuple<Mesh, int>(mesh, triangleIndex), out int index))
            {
                return index;
            }
            
            int baseIndex = 0;
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                int[] triangles = mesh.GetTriangles(i);
                int triangleCount = triangles.Length / 3;

                if (triangleIndex < baseIndex + triangleCount)
                {
                    SavedSubMeshIndexes.Add(new Tuple<Mesh, int>(mesh, triangleIndex), i);
                    return i;
                }

                baseIndex += triangleCount;
            }

            return -1;
        }
        
        #endregion
    }
}

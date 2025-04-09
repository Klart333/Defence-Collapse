using Random = UnityEngine.Random;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using WaveFunctionCollapse;

namespace Chunks
{
    public class TreeGrower : PooledMonoBehaviour
    {
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
        
        public bool HasGrown { get; set; }
        public Cell Cell { get; set; }
        
        //private List<PooledMonoBehaviour> spawnedTrees = new List<PooledMonoBehaviour>();

        public async UniTask GrowTrees()
        {
            Vector3 offset = raycastArea.ToXyZ(1).MultiplyByAxis(transform.localScale) / 2.0f;
            Vector3 min = transform.position - offset;
            Vector3 max = transform.position + offset;
            List<Vector2> positions = new List<Vector2>();
            
            for (int i = 0; i < raycastCount; i++)
            {
                Vector3 pos = new Vector3(Random.Range(min.x, max.x), 2, Random.Range(min.z, max.z));
                if (!CheckCollisionWithTrees(pos.XZ())) continue;

                Ray ray = new Ray(pos, Vector3.down);
                Material mat = GetHitMaterial(ray, out RaycastHit hit);

                if (mat == treeMaterial)
                {
                    pos.y = 0;
                    positions.Add(pos.XZ());
                    SpawnTree(hit.point);
                    await UniTask.Delay(100);
                }
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
            PooledMonoBehaviour tree = trees[Random.Range(0, trees.Length)];
            tree.GetAtPosAndRot<PooledMonoBehaviour>(pos, Quaternion.AngleAxis(Random.value * 360, Vector3.up));
            //spawnedTrees.Add(tree);
        }

        private static Material GetHitMaterial(Ray ray, out RaycastHit hit)
        {
            if (!Physics.Raycast(ray, out hit, 3)) 
                return null;

            if (hit.collider is not MeshCollider { sharedMesh: { } mesh }) 
                return null;

            int subMeshIndex = GetSubmeshIndex(mesh, hit.triangleIndex);
            if (subMeshIndex == -1) 
                return null;

            return hit.collider.TryGetComponent<Renderer>(out var renderer) && subMeshIndex < renderer.sharedMaterials.Length 
                ? renderer.sharedMaterials[subMeshIndex] 
                : null;
        }

        private static int GetSubmeshIndex(Mesh mesh, int triangleIndex)
        {
            int baseIndex = 0;

            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                int[] triangles = mesh.GetTriangles(i);
                int triangleCount = triangles.Length / 3;

                if (triangleIndex < baseIndex + triangleCount)
                    return i;

                baseIndex += triangleCount;
            }

            return -1;
        }

        public void Clear()
        {
            //for (int i = 0; i < spawnedTrees.Count; i++)
            //{
            //    spawnedTrees[i].gameObject.SetActive(false);
            //}
            //
            //spawnedTrees.Clear();
        }
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireCube(transform.position + Vector3.up, raycastArea.ToXyZ(1).MultiplyByAxis(transform.localScale));
        }
    }
}

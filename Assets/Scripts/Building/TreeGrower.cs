using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Buildings
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
        
        private void Start()
        {
            //GrowTrees();
        }

        [Button]
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
                Material mat = GetHitMaterial(ray);

                if (mat == treeMaterial)
                {
                    pos.y = 0;
                    positions.Add(pos.XZ());
                    SpawnTree(pos);
                    await UniTask.Delay(100);
                }
            }

            if (positions.Count <= 0)
            {
                await UniTask.Delay(100);
                GrowTrees().Forget(Debug.LogError);
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
        }

        private static Material GetHitMaterial(Ray ray)
        {
            if (!Physics.Raycast(ray, out RaycastHit hit, 3)) 
                return null;

            if (hit.collider is not MeshCollider { sharedMesh: { } mesh }) 
                return null;

            int subMeshIndex = GetSubmeshIndex(mesh, hit.triangleIndex);
            print("SubMeshIndex: " + subMeshIndex);
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

        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireCube(transform.position + Vector3.up, raycastArea.ToXyZ(1).MultiplyByAxis(transform.localScale));
        }
    }
}

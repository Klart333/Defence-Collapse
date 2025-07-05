using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;

namespace WaveFunctionCollapse
{
#if UNITY_EDITOR
    [CreateAssetMenu(fileName = "Mesh Raycaster", menuName = "WFC/Mesh Raycaster", order = 0)]
    public class MeshRaycaster : ScriptableObject, IMeshRayService
    {
        [SerializeField]
        private MaterialData materialData;
        
        [SerializeField]
        private MeshRaycastDummy meshRaycastDummy;

        [Title("Ray Settings")]
        [SerializeField]
        private LayerMask layerMask;
        
        private readonly Direction[] directions =
        {
            Direction.Right,
            Direction.Left,
            Direction.Forward,
            Direction.Backward,
        };
        
        public Dictionary<Direction, int[]> GetMeshIndices(Mesh mesh, Material[] mats)
        {
            MeshRaycastDummy dummy = meshRaycastDummy.SpawnMesh(mesh, mats);

            Dictionary<Direction, int[]> result = new Dictionary<Direction, int[]>();

            // Since mesh bounds are always 2 units centered at origin
            const float halfSize = 1f; // half of 2 units
            
            foreach (Direction direction in directions)
            {
                HashSet<int> submeshIndices = new HashSet<int>();

                // Ray should face TOWARD the mesh, so we invert the face direction
                Vector3 faceNormal = direction switch
                {
                    Direction.Right    => Vector3.right,
                    Direction.Left     => Vector3.left,
                    Direction.Forward  => Vector3.forward,
                    Direction.Backward => Vector3.back,
                    _ => Vector3.zero,
                };

                Vector3 rayDir = -faceNormal; // ray goes TOWARD the mesh
                Vector3 lateralAxis = direction switch
                {
                    Direction.Right    => Vector3.forward,
                    Direction.Left     => Vector3.back,
                    Direction.Forward  => Vector3.left,
                    Direction.Backward => Vector3.right,
                    _ => Vector3.zero,
                };

                Vector3[] lateralOffsets = {
                    lateralAxis * Mathf.Lerp(-halfSize, halfSize, 0.1f),  // Left side
                    lateralAxis * Mathf.Lerp(-halfSize, halfSize, 0.9f),  // Right side
                };

                foreach (Vector3 lateralOffset in lateralOffsets)
                {
                    Vector3 origin = faceNormal * (halfSize + 0.1f) + lateralOffset;
                    Ray ray = new Ray(origin, rayDir);

                    if (!Physics.Raycast(ray, out RaycastHit hit, 10f)) continue;
                    int triangleIndex = hit.triangleIndex;
                    int triangleStart = triangleIndex * 3;

                    for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
                    {
                        var desc = mesh.GetSubMesh(submesh);
                        if (triangleStart < desc.indexStart || triangleStart >= desc.indexStart + desc.indexCount) continue;
                        submeshIndices.Add(submesh);
                        break;
                    }
                }
                result.Add(direction, submeshIndices.Select(x => mats[x]).Select(x => materialData.Materials.IndexOf(x)).ToArray());
            }

            DestroyImmediate(dummy.gameObject);
            return result;
        }

    }
#endif
}
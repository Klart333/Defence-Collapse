using Sirenix.OdinInspector;
using UnityEngine;

namespace Utility
{
    public class SelectMeshOnEnable : MonoBehaviour
    {
        [Title("References")]
        [SerializeField]
        private MeshFilter meshFilter;

        [SerializeField]
        private Mesh[] meshes;

        private void OnEnable()
        {
            meshFilter.sharedMesh = meshes[Random.Range(0, meshes.Length)];
        }
    }
}
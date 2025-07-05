using UnityEngine;

namespace WaveFunctionCollapse
{
#if UNITY_EDITOR
    public class MeshRaycastDummy : MonoBehaviour
    {
        [SerializeField]
        private MeshFilter meshFilter;
        
        [SerializeField]
        private MeshRenderer meshRenderer;
        
        [SerializeField]
        private MeshCollider meshCollider;
        
        public MeshRaycastDummy SpawnMesh(Mesh mesh, Material[] mats)
        {
            MeshRaycastDummy dummy = Instantiate(this);
            
            dummy.meshFilter.sharedMesh = mesh;
            dummy.meshRenderer.sharedMaterials = mats;
            dummy.meshCollider.sharedMesh = mesh;
            return dummy;
        }
    }
#endif

}
using Sirenix.OdinInspector;
using UnityEngine;

namespace Variables
{
    [InlineEditor, CreateAssetMenu(fileName = "New Mesh Variable", menuName = "Variable/Mesh", order = 0)]
    public class MeshVariable : ScriptableObject
    {
        [Title("Mesh")]
        [SerializeField]
        private Mesh mesh;
        
        [SerializeField]
        private Material material;

        [SerializeField]
        private float scale = 0.1f;
        
        public Material Material => material;
        public float Scale => scale;
        public Mesh Mesh => mesh;
    }
}
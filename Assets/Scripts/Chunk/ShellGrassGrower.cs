using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Chunks
{
    public class ShellGrassGrower : PooledMonoBehaviour
    {
        private static readonly int ShellCount = Shader.PropertyToID("_ShellCount");
        private static readonly int ShellLength = Shader.PropertyToID("_ShellLength");
        private static readonly int Density = Shader.PropertyToID("_Density");
        private static readonly int Thickness = Shader.PropertyToID("_Thickness");
        private static readonly int Attenuation = Shader.PropertyToID("_Attenuation");
        private static readonly int ShellDistanceAttenuation = Shader.PropertyToID("_ShellDistanceAttenuation");
        private static readonly int Curvature = Shader.PropertyToID("_Curvature");
        private static readonly int DisplacementStrength = Shader.PropertyToID("_DisplacementStrength");
        private static readonly int OcclusionBias = Shader.PropertyToID("_OcclusionBias");
        private static readonly int ShellColor = Shader.PropertyToID("_ShellColor");
        private static readonly int ShellIndex = Shader.PropertyToID("_ShellIndex");
        private static readonly int ShellDirection = Shader.PropertyToID("_ShellDirection");
        
        [Title("Shader Settings")]
        [SerializeField]
        private Material material;
        
        [Range(1, 256)]
        [SerializeField]
        private int shellCount = 32;

        [Range(0.0f, 1.0f)]
        [SerializeField]
        private float shellLength = 0.15f;

        [Range(0.01f, 3.0f)]
        [SerializeField]
        private float distanceAttenuation = 1.0f;

        [Range(1.0f, 200.0f)]
        [SerializeField]
        private float density = 48.0f;

        [Range(5.0f, 20.0f)]
        [SerializeField]
        private float thickness = 10.0f;

        [Range(0.0f, 10.0f)]
        [SerializeField]
        private float curvature = 0.0f;

        [Range(0.0f, 1.0f)]
        [SerializeField]
        private float displacementStrength = 0.1f;
        
        [SerializeField] 
        private Color shellColor;

        [Range(0.0f, 5.0f)]
        [SerializeField]
        private float occlusionAttenuation = 1.0f;
    
        [Range(0.0f, 1.0f)]
        [SerializeField]
        private float occlusionBias = 0.0f;
        
        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;
        private MaterialPropertyBlock block;

        private int submeshIndex;
        
        public int3 ChunkKey { get; set; }
        public Cell Cell { get; set; }
        
        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            meshFilter = GetComponent<MeshFilter>();

            block = new MaterialPropertyBlock();
            meshRenderer.GetPropertyBlock(block);
            block.SetInt(ShellCount, shellCount);
            block.SetFloat(ShellLength, shellLength);
            block.SetFloat(Density, density);
            block.SetFloat(Thickness, thickness);
            block.SetFloat(Attenuation, occlusionAttenuation);
            block.SetFloat(ShellDistanceAttenuation, distanceAttenuation);
            block.SetFloat(Curvature, curvature);
            block.SetFloat(DisplacementStrength, displacementStrength);
            block.SetFloat(OcclusionBias, occlusionBias);
            block.SetVector(ShellColor, shellColor);
        }

        public void DisplayGrass(Mesh mesh, int submeshIndex)
        {
            meshFilter.sharedMesh = mesh;
            this.submeshIndex = submeshIndex;
        }

        private void Update()
        {
            for (int i = 0; i < shellCount; i++)
            {
                block.SetFloat(ShellIndex, i); 
                meshRenderer.SetPropertyBlock(block);
                Graphics.DrawMesh(meshFilter.sharedMesh, transform.localToWorldMatrix, material, 0, null, submeshIndex, block, ShadowCastingMode.Off, false);
            }
        }
    }
}
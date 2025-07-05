using UnityEngine;

public class SimpleShell : MonoBehaviour {
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
    
    public Mesh shellMesh;
    public Shader shellShader;

    [Range(1, 256)]
    public int shellCount = 16;

    [Range(0.0f, 1.0f)]
    public float shellLength = 0.15f;

    [Range(0.01f, 3.0f)]
    public float distanceAttenuation = 1.0f;

    [Range(1.0f, 1000.0f)]
    public float density = 100.0f;

    [Range(0.0f, 10.0f)]
    public float thickness = 1.0f;

    [Range(0.0f, 10.0f)]
    public float curvature = 1.0f;

    [Range(0.0f, 1.0f)]
    public float displacementStrength = 0.1f;

    public Color shellColor;

    [Range(0.0f, 5.0f)]
    public float occlusionAttenuation = 1.0f;
    
    [Range(0.0f, 1.0f)]
    public float occlusionBias = 0.0f;

    private Material shellMaterial;
    private GameObject shell;

    private MaterialPropertyBlock block;

    void OnEnable()
    {
        block = new MaterialPropertyBlock();
        
        shellMaterial = new Material(shellShader);

        shell = new GameObject("Shell");
        shell.transform.SetParent(transform, false);
        shell.AddComponent<MeshFilter>().mesh = shellMesh;
        MeshRenderer meshRenderer = shell.AddComponent<MeshRenderer>();
        meshRenderer.material = shellMaterial;

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
        
        for (int i = 0; i < shellCount; i++)
        {
            block.SetFloat(ShellIndex, i); 
            meshRenderer.SetPropertyBlock(block);
            Graphics.DrawMesh(shellMesh, transform.localToWorldMatrix, shellMaterial, 0, null, 0, block);
        }
    }

    private void Update()
    {
        MeshRenderer meshRenderer = shell.GetComponent<MeshRenderer>();
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
        
        for (int i = 0; i < shellCount; i++)
        {
            block.SetFloat(ShellIndex, i); 
            meshRenderer.SetPropertyBlock(block);
            Graphics.DrawMesh(shellMesh, transform.localToWorldMatrix, shellMaterial, 0, null, 0, block);
        }
    }
}

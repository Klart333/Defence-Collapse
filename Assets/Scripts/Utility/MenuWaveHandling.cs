using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using System;
using UnityEngine;
using WaveFunctionCollapse;
using Random = UnityEngine.Random;

public class MenuWaveHandling : MonoBehaviour
{
    [Title("Wave Function")]
    [SerializeField, MinMaxSlider(5, 50)]
    private Vector2Int minMaxXZ;

    [SerializeField, MinMaxSlider(4, 10)]
    private Vector2Int minMaxY;

    [Title("Camera")]
    [SerializeField]
    private Transform midPoint;

    [SerializeField]
    private float spinSpeed = 2;

    [SerializeField]
    private float camOffset = 10;

    [SerializeField]
    private float camVerticalOffset = 5;

    private float angle = 0;

    private Camera cam;    
    private GroundGenerator groundGenerator;

    private void Awake()
    {
        cam = Camera.main;

        groundGenerator = FindAnyObjectByType<GroundGenerator>();
        groundGenerator.OnMapGenerated += WaveFunction_OnMapGenerated;

        midPoint.position = new Vector3(groundGenerator.WaveFunction.GridSize.x * groundGenerator.WaveFunction.GridScale.x / 2.0f, groundGenerator.WaveFunction.GridSize.y * groundGenerator.WaveFunction.GridScale.y / 2.0f, groundGenerator.WaveFunction.GridSize.z * groundGenerator.WaveFunction.GridScale.z / 2.0f);
    }

    private void OnDisable()
    {
        groundGenerator.OnMapGenerated -= WaveFunction_OnMapGenerated;
    }

    private async void WaveFunction_OnMapGenerated()
    {
        await UniTask.Delay(TimeSpan.FromSeconds(5));

        int xz = Random.Range(minMaxXZ.x, minMaxXZ.y);
        groundGenerator.WaveFunction.SetGridSize(new Vector3Int(xz, Random.Range(minMaxY.x, minMaxY.y), xz));
        groundGenerator.GetComponent<MeshFilter>().sharedMesh = null;  
        _ = groundGenerator.Run();

        midPoint.position = new Vector3(groundGenerator.WaveFunction.GridSize.x * groundGenerator.WaveFunction.GridScale.x / 2.0f, groundGenerator.WaveFunction.GridSize.y * groundGenerator.WaveFunction.GridScale.y / 2.0f, groundGenerator.WaveFunction.GridSize.z * groundGenerator.WaveFunction.GridScale.z / 2.0f);
    }

    private void Update()
    {
        Vector3 dir = (midPoint.transform.position - cam.transform.position).normalized;

        cam.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

        Vector3 horizontal = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * camOffset;
        cam.transform.position = midPoint.transform.position + horizontal + Vector3.up * camVerticalOffset;

        angle += Time.deltaTime * spinSpeed * Mathf.Deg2Rad;
    }
}

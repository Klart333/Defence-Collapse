using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class DistrictGenerator : MonoBehaviour
{
    [Title("Wave Function")]
    [SerializeField]
    private ChunkWaveFunction waveFunction;

    [SerializeField]
    private Vector3Int chunkSize;
    
    [Title("Data")]
    [SerializeField]
    private BuildableCornerData buildableCornerData;
    
    [Title("Settings")]
    [SerializeField]
    private int chillTimeMs;
    
    [SerializeField]
    private float maxMillisecondsPerFrame = 4;

    private readonly Vector2Int[] corners = new Vector2Int[4]
    {
        new Vector2Int(-1, -1),
        new Vector2Int(1, -1),
        new Vector2Int(1, 1),
        new Vector2Int(-1, 1),
    };
    
    public ChunkWaveFunction WaveFunction => waveFunction;

    private void OnEnable()
    {
        waveFunction.Load();
        //waveFunction.LoadChunk(Vector3.zero, new Vector3Int(2, 2, 2));
        //waveFunction.LoadChunk(new Vector3(2, 0, 0), new Vector3Int(2, 2, 2));
        //waveFunction.LoadChunk(new Vector3(2, 0, 2), new Vector3Int(2, 2, 2));
        //waveFunction.LoadChunk(new Vector3(0, 0, 2), new Vector3Int(2, 2, 2));
        //waveFunction.LoadChunk(new Vector3(0, 2, 2), new Vector3Int(2, 2, 2));
        //waveFunction.LoadChunk(new Vector3(2, 2, 2), new Vector3Int(2, 2, 2));
        //waveFunction.LoadChunk(new Vector3(2, 2, 0), new Vector3Int(2, 2, 2));
        //_ = Run();

        Events.OnBuildingBuilt += OnBuildingBuilt;
    }

    private void OnDisable()
    {
        Events.OnBuildingBuilt -= OnBuildingBuilt;   
    }

    private void OnBuildingBuilt(IEnumerable<IBuildable> buildables)
    {
        Vector3 offset = new Vector3(waveFunction.GridScale.x, 0 , waveFunction.GridScale.z) / -2.0f;
        foreach (IBuildable buildable in buildables)
        {
            for (int i = 0; i < corners.Length; i++)
            {
                if (!buildableCornerData.IsBuildable(buildable.MeshRot, corners[i])) continue;
                
                Vector3 pos = buildable.gameObject.transform.position + new Vector3(corners[i].x * chunkSize.x * waveFunction.GridScale.x, 0, corners[i].y * chunkSize.z * waveFunction.GridScale.z) / -2.0f + offset;
                waveFunction.LoadChunk(pos, chunkSize);
            }
        }

        _ = Run();
    }

    public async UniTask Run()
    {
        Stopwatch watch = Stopwatch.StartNew();
        while (!waveFunction.AllCollapsed())
        {
            waveFunction.Iterate(); // Does not need to await

            if (watch.ElapsedMilliseconds < maxMillisecondsPerFrame) continue;
            
            await UniTask.NextFrame();
            if (chillTimeMs > 0)
            {
                await UniTask.Delay(chillTimeMs);
            }

            watch.Restart();
        }

        Debug.Log("Done");
    }

    
    #region Debug

    public void OnDrawGizmosSelected()
    {
        if (!UnityEditor.EditorApplication.isPlaying)
        {
            return;
        }

        foreach (var chunk in waveFunction.Chunks)
        {
            foreach (var cell in chunk.Cells)
            {
                Vector3 pos = cell.Position;
                Gizmos.color = cell.Buildable ? Color.white : Color.red;
                Gizmos.DrawWireCube(pos, new Vector3(waveFunction.GridScale.x, waveFunction.GridScale.y, waveFunction.GridScale.z) * 0.75f);
            }
        }
    }
    
    #endregion
}
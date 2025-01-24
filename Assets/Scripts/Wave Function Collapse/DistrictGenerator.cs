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
    
    [Title("Settings")]
    [SerializeField]
    private int chillTimeMs;
    
    [SerializeField]
    private float maxMillisecondsPerFrame = 4;

    public ChunkWaveFunction WaveFunction => waveFunction;

    private void OnEnable()
    {
        waveFunction.Load();
        waveFunction.LoadChunk(Vector3.zero, new Vector3Int(10, 2, 10));
        waveFunction.LoadChunk(new Vector3(10, 0, 0), new Vector3Int(10, 2, 10));
        waveFunction.LoadChunk(new Vector3(10, 0, 10), new Vector3Int(10, 2, 10));
        _ = Run();

        Events.OnBuildingBuilt += OnBuildingBuilt;
    }

    private void OnDisable()
    {
        Events.OnBuildingBuilt -= OnBuildingBuilt;   
    }

    private void OnBuildingBuilt(IEnumerable<IBuildable> buildables)
    {
        foreach (IBuildable buildable in buildables)
        {
            waveFunction.LoadChunk(buildable.gameObject.transform.position, chunkSize);
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
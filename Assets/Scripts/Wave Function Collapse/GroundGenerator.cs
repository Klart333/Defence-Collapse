using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using System.Diagnostics;
using UnityEngine;
using System;

public class GroundGenerator : MonoBehaviour
{
    public event Action OnMapGenerated;
    
    [Title("Wave Function")]
    [SerializeField]
    private WaveFunction waveFunction;

    [Title("Settings")]
    [SerializeField]
    private int chillTimeMs;
    
    [SerializeField]
    private float maxMillisecondsPerFrame = 4;
    
    [SerializeField]
    private bool shouldCombine = true;

    public WaveFunction WaveFunction => waveFunction;
    
    private void Start()
    {
        _ = Run();
    }

    public async UniTask Run()
    {
        if (!waveFunction.Load())
        {
            return;
        }

        //await PredeterminePath();
        await BottomBuildUp();

        Stopwatch watch = Stopwatch.StartNew();
        while (!waveFunction.AllCollapsed)
        {
            waveFunction.Iterate(); // Does not need to await

            if (!(watch.ElapsedMilliseconds > maxMillisecondsPerFrame)) continue;
            
            await UniTask.NextFrame();
            if (chillTimeMs > 0)
            {
                await UniTask.Delay(chillTimeMs);
            }

            watch.Restart();
        }

        if (shouldCombine)
        {
            CombineMeshes();
        }

        OnMapGenerated?.Invoke();
    }
    
    private async UniTask BottomBuildUp()
    {
        for (int x = 0; x < waveFunction.GridSize.x; x++)
        {
            for (int z = 0; z < waveFunction.GridSize.z; z++)
            {
                if ((x == 0 || x == waveFunction.GridSize.x - 1) && (z == 0 || z == waveFunction.GridSize.z - 1))
                {
                    await PlaceGround(x, z);
                    continue;
                }

                if ((x != 0 && x != waveFunction.GridSize.x - 1) && (z != 0 && z != waveFunction.GridSize.z - 1))
                {
                    continue;
                }

                if (UnityEngine.Random.value < 0.8f)
                {
                    continue;
                }

                await PlaceGround(x, z);
            }
        }

        return;

        async UniTask PlaceGround(int x, int z)
        {
            int index = waveFunction.GetIndex(x, 0, z);
            if (waveFunction.Cells[index].PossiblePrototypes.Contains(waveFunction.Prototypes[2 * 4]))
            {
                waveFunction.SetCell(index, waveFunction.Prototypes[2 * 4]);

                waveFunction.Propagate();

                await UniTask.Yield();
            }
        }
    }
    

    private void CombineMeshes()
    {
        GetComponent<MeshCombiner>().CombineMeshes();
    }
}
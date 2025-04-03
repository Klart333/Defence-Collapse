using System;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;
using WaveFunctionCollapse;
using Random = UnityEngine.Random;

namespace Utility
{
    public class MenuWaveGenerator : MonoBehaviour
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

        [Title("Debug")]
        [SerializeField]
        private bool shouldRun = true;
        
        public WaveFunction WaveFunction => waveFunction;

        private void Start()
        {
            if (shouldRun)
                _ = Run();
        }

        public async UniTask Run()
        {
            waveFunction.ParentTransform = transform;
            if (!waveFunction.Load())
            {
                return;
            }

            //await PredeterminePath();
            await BottomBuildUp();

            Stopwatch watch = Stopwatch.StartNew();
            while (!waveFunction.AllCollapsed)
            {
                waveFunction.Iterate();

                if (watch.ElapsedMilliseconds < maxMillisecondsPerFrame) continue;

                await UniTask.NextFrame();
                if (chillTimeMs > 0)
                {
                    await UniTask.Delay(chillTimeMs);
                }

                watch.Restart();
            }

            OnMapGenerated?.Invoke();
        }

        private async UniTask BottomBuildUp()
        {
            for (int x = 0; x < waveFunction.GridSize.x; x++)
            for (int z = 0; z < waveFunction.GridSize.z; z++)
            {
                if ((x == 0 || x == waveFunction.GridSize.x - 1) && (z == 0 || z == waveFunction.GridSize.z - 1))
                {
                    PlaceGround(x, z);
                    await UniTask.Yield();
                    continue;
                }

                if ((x != 0 && x != waveFunction.GridSize.x - 1) && (z != 0 && z != waveFunction.GridSize.z - 1))
                {
                    continue;
                }

                if (Random.value < 0.8f)
                {
                    continue;
                }

                PlaceGround(x, z);
                await UniTask.Yield();
            }
        

            return;

            void PlaceGround(int x, int z)
            {
                int index = waveFunction.GetIndex(x, 0, z);
                if (waveFunction.Cells[index].PossiblePrototypes.Contains(waveFunction.Prototypes[0]))
                {
                    waveFunction.SetCell(index, waveFunction.Prototypes[0]);

                    waveFunction.Propagate();
                }
            }
        }

    }
}
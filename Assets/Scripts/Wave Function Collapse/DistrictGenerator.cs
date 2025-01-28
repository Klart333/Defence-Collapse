using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using Sirenix.Utilities;
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
    private int awaitEveryFrame = 1;

    [SerializeField, ShowIf(nameof(ShouldAwait))]
    private int awaitTimeMs = 1;
    
    [SerializeField]
    private float maxMillisecondsPerFrame = 4;
    
    [Title("Debug")]
    [SerializeField]
    private bool debug = false;
    
    private bool ShouldAwait => awaitEveryFrame > 0;
    
    private readonly Queue<List<IBuildable>> buildQueue = new Queue<List<IBuildable>>();
    
    private readonly Vector2Int[] corners = new Vector2Int[4]
    {
        new Vector2Int(-1, -1),
        new Vector2Int(1, -1),
        new Vector2Int(1, 1),
        new Vector2Int(-1, 1),
    };

    private Vector3 offset;
    
    private bool isUpdatingChunks = false;
    
    public ChunkWaveFunction WaveFunction => waveFunction;

    private void OnEnable()
    {
        offset = new Vector3(waveFunction.GridScale.x, 0 , waveFunction.GridScale.z) / -2.0f;

        waveFunction.Load();
        Events.OnBuildingBuilt += OnBuildingBuilt;
    }

    private void OnDisable()
    {
        Events.OnBuildingBuilt -= OnBuildingBuilt;   
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            foreach (Chunk chunk in waveFunction.Chunks)
            {
                chunk.Clear(waveFunction.GameObjectPool);
                waveFunction.LoadCells(chunk);
            }

            Run();
        }
    }

    private void OnBuildingBuilt(IEnumerable<IBuildable> buildables)
    {
        buildQueue.Enqueue(new List<IBuildable>(buildables));

        if (!isUpdatingChunks)
        {
            UpdateChunks();
        }
    }

    private async UniTask UpdateChunks()
    {
        isUpdatingChunks = true;

        while (buildQueue.TryDequeue(out List<IBuildable> buildables))
        {
            List<Vector3> positions = new List<Vector3>();
            HashSet<int> overrideChunks = new HashSet<int>();
            foreach (IBuildable buildable in buildables)
            {
                for (int i = 0; i < corners.Length; i++)
                {
                    if (buildable.MeshRot.Mesh != null && !buildableCornerData.IsBuildable(buildable.MeshRot, corners[i])) continue;
                
                    Vector3 pos = buildable.gameObject.transform.position + new Vector3(corners[i].x * chunkSize.x * waveFunction.GridScale.x, 0, corners[i].y * chunkSize.z * waveFunction.GridScale.z) / -2.0f + offset;
                    if (waveFunction.CheckChunkOverlap(pos, chunkSize, out Chunk chunk))
                    {
                        overrideChunks.Add(chunk.ChunkIndex);
                    }
                    else
                    {
                        positions.Add(pos);
                    }
                }
            }
        
            foreach (int chunkIndex in overrideChunks)
            {
                waveFunction.LoadCells(waveFunction.Chunks[chunkIndex]);
            }
        
            foreach (Vector3 pos in positions)
            {
                overrideChunks.Add(waveFunction.LoadChunk(pos, chunkSize).ChunkIndex);
            }

            waveFunction.Propagate();
            await Run();
            await UniTask.Yield();

            while (CheckFailed(overrideChunks))
            {
                HashSet<int> neighbours = new HashSet<int>();
                foreach (int chunkIndex in overrideChunks)
                {
                    for (int i = 0; i < waveFunction.Chunks[chunkIndex].AdjacentChunks.Length; i++)
                    {
                        Chunk chunk = waveFunction.Chunks[chunkIndex].AdjacentChunks[i];
                        if (chunk == null || overrideChunks.Contains(chunk.ChunkIndex)) continue;
                        
                        neighbours.Add(chunk.ChunkIndex);
                    }
                }
                overrideChunks.AddRange(neighbours);

                foreach (int chunkIndex in overrideChunks)
                {
                    waveFunction.Chunks[chunkIndex].Clear(waveFunction.GameObjectPool);
                    waveFunction.LoadCells(waveFunction.Chunks[chunkIndex]);
                }
                
                await Run();
                await UniTask.Yield();
            }
        }
     
        isUpdatingChunks = false;
    }

    private bool CheckFailed(IEnumerable<int> overrideChunks)
    {
        int minValid = 2;
        int count = 0;
        foreach (int chunkIndex in overrideChunks)
        {
            count++;
            foreach (Cell cell in waveFunction.Chunks[chunkIndex].Cells)
            {
                if (cell.PossiblePrototypes[0].MeshRot.Mesh != null)
                {
                    minValid--;
                    break;
                }
            }
            
            if (minValid <= 0)
            {
                break;
            }
        }
        return count > 2 && minValid > 0;
    }

    public async UniTask Run()
    {
        Stopwatch watch = Stopwatch.StartNew();
        int frameCount = 0;
        while (!waveFunction.AllCollapsed())
        {
            watch.Start();
            waveFunction.Iterate();
            watch.Stop();
            
            if (awaitEveryFrame > 0 && ++frameCount % awaitEveryFrame == 0)
            {
                frameCount = 0;
                await UniTask.Delay(awaitTimeMs);
            }
            
            if (watch.ElapsedMilliseconds < maxMillisecondsPerFrame) continue;

            await UniTask.NextFrame();
            
            watch.Restart();
        }
    }

    
    #region Debug

    public void OnDrawGizmosSelected()
    {
        if (!UnityEditor.EditorApplication.isPlaying || !debug)
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
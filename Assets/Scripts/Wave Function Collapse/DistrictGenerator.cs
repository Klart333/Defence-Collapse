using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using Sirenix.Utilities;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class DistrictGenerator : MonoBehaviour, IChunkWaveFunction
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
    
    public ChunkWaveFunction ChunkWaveFunction => waveFunction;

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

            Run().Forget(ex =>
            {
                Debug.LogError($"Async function failed: {ex}");
            });;
        }
    }

    private void OnBuildingBuilt(IEnumerable<IBuildable> buildables)
    {
        buildQueue.Enqueue(new List<IBuildable>(buildables));

        if (!isUpdatingChunks)
        {
            UpdateChunks().Forget(ex =>
            {
                Debug.LogError($"Async function failed: {ex}");
            });;
        }
    }

    private async UniTask UpdateChunks()
    {
        isUpdatingChunks = true;

        while (buildQueue.TryDequeue(out List<IBuildable> buildables))
        {
            List<Vector3> positions = new List<Vector3>();
            HashSet<Chunk> overrideChunks = new HashSet<Chunk>();
            foreach (IBuildable buildable in buildables)
            {
                for (int i = 0; i < corners.Length; i++)
                {
                    bool isBuildable = buildableCornerData.IsBuildable(buildable.MeshRot, corners[i], out bool meshIsBuildable);
                    isBuildable |= buildable.MeshRot.Mesh == null;
                    if (!isBuildable && !meshIsBuildable) continue;
                
                    Vector3 pos = buildable.gameObject.transform.position + new Vector3(corners[i].x * chunkSize.x * waveFunction.GridScale.x, 0, corners[i].y * chunkSize.z * waveFunction.GridScale.z) / -2.0f + offset;
                    if (waveFunction.CheckChunkOverlap(pos, chunkSize, out Chunk chunk))
                    {
                        if (isBuildable)
                        {
                            chunk.Clear(waveFunction.GameObjectPool);
                            overrideChunks.Add(chunk);
                        }
                        else
                        {
                            waveFunction.RemoveChunk(chunk.ChunkIndex, out List<Chunk> neighbourChunks);
                            for (int j = 0; j < neighbourChunks.Count; j++)
                            {
                                neighbourChunks[j].Clear(waveFunction.GameObjectPool);
                                overrideChunks.Add(neighbourChunks[j]);
                                for (int k = 0; k < neighbourChunks[j].AdjacentChunks.Length; k++)
                                {
                                    if (neighbourChunks[j].AdjacentChunks[k] == null) continue;
                                    
                                    neighbourChunks[j].AdjacentChunks[k].Clear(waveFunction.GameObjectPool);
                                    overrideChunks.Add(neighbourChunks[j].AdjacentChunks[k]);
                                }
                            }
                        }
                    }
                    else if (isBuildable)
                    {
                        positions.Add(pos);
                    }
                }
            }
        
            foreach (Chunk chunk in overrideChunks)
            {
                if (chunk.IsRemoved)
                {
                    continue;
                }
                
                waveFunction.LoadCells(chunk);
            }
        
            foreach (Vector3 pos in positions)
            {
                overrideChunks.Add(waveFunction.LoadChunk(pos, chunkSize));
            }

            waveFunction.Propagate();
            await Run();
            await UniTask.Yield();

            while (CheckFailed(overrideChunks))
            {
                HashSet<Chunk> neighbours = new HashSet<Chunk>();
                foreach (Chunk overrideChunk in overrideChunks)
                {
                    for (int i = 0; i < overrideChunk.AdjacentChunks.Length; i++)
                    {
                        Chunk chunk = overrideChunk.AdjacentChunks[i];
                        if (chunk == null || overrideChunks.Contains(overrideChunk)) continue;
                        
                        neighbours.Add(overrideChunk);
                    }
                }
                overrideChunks.AddRange(neighbours);

                foreach (Chunk chunk in overrideChunks)
                {
                    chunk.Clear(waveFunction.GameObjectPool);
                    waveFunction.LoadCells(chunk);
                }
                
                await Run();
                await UniTask.Yield();
            }
        }
     
        isUpdatingChunks = false;
    }

    private bool CheckFailed(IEnumerable<Chunk> overrideChunks)
    {
        int minValid = 2;
        int count = 0;
        foreach (Chunk chunk in overrideChunks)
        {
            count++;
            foreach (Cell cell in chunk.Cells)
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

    private async UniTask Run()
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
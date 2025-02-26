using System.Collections.Generic;
using Debug = UnityEngine.Debug;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using System.Diagnostics;
using Buildings.District;
using Sirenix.Utilities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace WaveFunctionCollapse
{
    public class DistrictGenerator : MonoBehaviour, IChunkWaveFunction
    {
        [Title("Wave Function")]
        [SerializeField]
        private ChunkWaveFunction waveFunction;

        [SerializeField]
        private Vector3Int chunkSize;
        
        [SerializeField]
        private PrototypeInfoData defaultPrototypeInfoData;

        [Title("Data")]
        [SerializeField]
        private BuildableCornerData buildableCornerData;

        [SerializeField]
        private DistrictHandler districtHandler;
        
        [Title("Settings")]
        [SerializeField]
        private int awaitEveryFrame = 1;

        [SerializeField, ShowIf(nameof(ShouldAwait))]
        private int awaitTimeMs = 1;

        [SerializeField]
        private float maxMillisecondsPerFrame = 4;

        [Title("Debug")]
        [SerializeField]
        private bool debug;

        private bool ShouldAwait => awaitEveryFrame > 0;

        private readonly Queue<List<IBuildable>> buildQueue = new Queue<List<IBuildable>>();

        private readonly Vector2Int[] corners =
        {
            new Vector2Int(-1, -1),
            new Vector2Int(1, -1),
            new Vector2Int(1, 1),
            new Vector2Int(-1, 1),
        };

        private Vector3 offset;

        private bool isUpdatingChunks;
        private bool isRunning;

        public ChunkWaveFunction ChunkWaveFunction => waveFunction;
        public Vector3 ChunkScale => new Vector3(chunkSize.x * ChunkWaveFunction.GridScale.x, chunkSize.y * ChunkWaveFunction.GridScale.y, chunkSize.z * ChunkWaveFunction.GridScale.z);
        public Vector3Int ChunkSize => chunkSize;

        private void OnEnable()
        {
            offset = new Vector3(waveFunction.GridScale.x, 0, waveFunction.GridScale.z) / -2.0f;

            waveFunction.Load(this);
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
                foreach (Chunk chunk in waveFunction.Chunks.Values)
                {
                    chunk.Clear(waveFunction.GameObjectPool);
                }
                
                foreach (Chunk chunk in waveFunction.Chunks.Values)
                {
                    waveFunction.LoadCells(chunk, chunk.PrototypeInfoData);
                }

                Run().Forget(ex => { Debug.LogError($"Async function failed: {ex}"); });
            }
        }

        private void OnBuildingBuilt(IEnumerable<IBuildable> buildables)
        {
            buildQueue.Enqueue(new List<IBuildable>(buildables));

            if (!isUpdatingChunks)
            {
                UpdateChunks().Forget(ex => { Debug.LogError($"Async function failed: {ex}"); });
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
                        bool isBuildable = buildableCornerData.IsCornerBuildable(buildable.MeshRot, corners[i], out bool meshIsBuildable);
                        isBuildable |= buildable.MeshRot.Mesh == null;
                        if (!isBuildable && !meshIsBuildable) continue;

                        Vector3 pos = buildable.gameObject.transform.position + new Vector3(corners[i].x * chunkSize.x * waveFunction.GridScale.x, 0, corners[i].y * chunkSize.z * waveFunction.GridScale.z) / -2.0f + offset;
                        int3 index = ChunkWaveUtility.GetDistrictIndex3(pos, ChunkScale);

                        if (waveFunction.Chunks.TryGetValue(index, out Chunk chunk))
                        {
                            if (districtHandler.IsBuilt(chunk))
                            {
                                continue;
                            }
                            
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
                                    ResetNeighbours(overrideChunks, neighbourChunks[j], 1);
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

                    waveFunction.LoadCells(chunk, defaultPrototypeInfoData);
                }

                foreach (Vector3 pos in positions)
                {
                    overrideChunks.Add(waveFunction.LoadChunk(pos, chunkSize, defaultPrototypeInfoData));
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
                        waveFunction.LoadCells(chunk, defaultPrototypeInfoData);
                    }

                    await Run();
                    await UniTask.Yield();
                }
            }

            isUpdatingChunks = false;
        }

        private void ResetNeighbours(HashSet<Chunk> overrideChunks, Chunk neighbourChunk, int depth)
        {
            for (int i = 0; i < neighbourChunk.AdjacentChunks.Length; i++)
            {
                if (neighbourChunk.AdjacentChunks[i] == null) continue;

                if (overrideChunks.Add(neighbourChunk.AdjacentChunks[i]))
                {
                    neighbourChunk.AdjacentChunks[i].Clear(waveFunction.GameObjectPool);
                    if (depth > 0)
                    {
                        ResetNeighbours(overrideChunks, neighbourChunk.AdjacentChunks[i], depth - 1);
                    }
                }
            }
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

        public async UniTask Run()
        {
            if (isRunning) return;

            isRunning = true;
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

            isRunning = false;
        }

        #region Debug

        public void OnDrawGizmosSelected()
        {
            if (!EditorApplication.isPlaying || !debug)
            {
                return;
            }

            foreach (Chunk chunk in waveFunction.Chunks.Values)
            {
                foreach (Cell cell in chunk.Cells)
                {
                    Vector3 pos = cell.Position;
                    Gizmos.color = cell.Buildable ? Color.white : Color.red;
                    Gizmos.DrawWireCube(pos, new Vector3(waveFunction.GridScale.x, waveFunction.GridScale.y, waveFunction.GridScale.z) * 0.75f);
                }
            }
        }

        #endregion
    }
}
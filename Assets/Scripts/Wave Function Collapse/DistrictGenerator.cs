using System.Collections.Generic;
using Debug = UnityEngine.Debug;
using Cysharp.Threading.Tasks;
using Sirenix.Serialization;
using Sirenix.OdinInspector;
using System.Diagnostics;
using Buildings.District;
using Sirenix.Utilities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using System;

namespace WaveFunctionCollapse
{
    public class DistrictGenerator : SerializedMonoBehaviour, IChunkWaveFunction<Chunk>
    {
        public event Action<Chunk> OnDistrictChunkRemoved;
        
        [Title("Wave Function")]
        [SerializeField]
        private ChunkWaveFunction<Chunk> waveFunction;

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

        [OdinSerialize, ReadOnly]
        public readonly Dictionary<ChunkIndex, List<int3>> ChunkIndexToChunks = new Dictionary<ChunkIndex, List<int3>>();
        
        private readonly Queue<List<IBuildable>> buildQueue = new Queue<List<IBuildable>>();

        private Vector3 offset;

        private bool isUpdatingChunks;
        
        private readonly Vector2Int[] corners =
        {
            new Vector2Int(-1, -1),
            new Vector2Int(1, -1),
            new Vector2Int(1, 1),
            new Vector2Int(-1, 1),
        };

        public Vector3 ChunkScale => new Vector3(chunkSize.x * ChunkWaveFunction.CellSize.x, chunkSize.y * ChunkWaveFunction.CellSize.y, chunkSize.z * ChunkWaveFunction.CellSize.z);
        public ChunkWaveFunction<Chunk> ChunkWaveFunction => waveFunction;
        private bool ShouldAwait => awaitEveryFrame > 0;
        public bool IsGenerating {get; private set;}
        public Vector3Int ChunkSize => chunkSize;

        private void OnEnable()
        {
            offset = new Vector3(waveFunction.CellSize.x, 0, waveFunction.CellSize.z) / -2.0f;

            waveFunction.Load(this);
            Events.OnBuildingBuilt += OnBuildingBuilt;
        }

        private void OnDisable()
        {
            Events.OnBuildingBuilt -= OnBuildingBuilt;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
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
                UpdateChunks().Forget();
            }
        }

        private async UniTaskVoid UpdateChunks()
        {
            isUpdatingChunks = true;

            while (buildQueue.TryDequeue(out List<IBuildable> buildables))
            {
                List<Vector3> positions = new List<Vector3>();
                HashSet<Chunk> overrideChunks = new HashSet<Chunk>();
                foreach (IBuildable buildable in buildables)
                {
                    HandleBuildable(buildable, overrideChunks, positions);
                }

                foreach (Chunk chunk in overrideChunks)
                {
                    if (chunk.IsRemoved)
                    {
                        continue;
                    }

                    waveFunction.LoadCells(chunk, chunk.PrototypeInfoData);
                }

                foreach (Vector3 pos in positions)
                {
                    overrideChunks.Add(waveFunction.LoadChunk(pos, chunkSize, defaultPrototypeInfoData));
                }

                waveFunction.Propagate();
                await Run();
                await UniTask.Yield();

                await IterativeFailChecks(overrideChunks);
            }

            isUpdatingChunks = false;

            void HandleBuildable(IBuildable buildable, HashSet<Chunk> overrideChunks, List<Vector3> positions)
            {
                for (int i = 0; i < corners.Length; i++)
                {
                    bool isBuildable = buildableCornerData.IsCornerBuildable(buildable.MeshRot, corners[i], out bool meshIsBuildable);
                    isBuildable |= buildable.MeshRot.MeshIndex == -1;
                    if (!isBuildable && !meshIsBuildable) continue;

                    Vector3 pos = buildable.gameObject.transform.position + new Vector3(corners[i].x * chunkSize.x * waveFunction.CellSize.x, 0, corners[i].y * chunkSize.z * waveFunction.CellSize.z) / -2.0f + offset;
                    int3 index = ChunkWaveUtility.GetDistrictIndex3(pos, ChunkScale);
                    if (waveFunction.Chunks.TryGetValue(index, out Chunk chunk))
                    {
                        Queue<Chunk> chunkQueue = new Queue<Chunk>();
                        chunkQueue.Enqueue(chunk);

                        while (chunkQueue.TryDequeue(out chunk))
                        {
                            bool isDistrict = districtHandler.IsBuilt(chunk);
                            if (isDistrict)
                            {
                                int3 aboveIndex = chunk.ChunkIndex + new int3(0, 1, 0);
                                if (waveFunction.Chunks.TryGetValue(aboveIndex, out Chunk aboveChunk))
                                {
                                    chunkQueue.Enqueue(aboveChunk);
                                }
                            }
                            
                            if (isBuildable)
                            {
                                chunk.Clear(waveFunction.GameObjectPool);
                                overrideChunks.Add(chunk);
                            }
                            else
                            {
                                OnDistrictChunkRemoved?.Invoke(chunk);
                                waveFunction.RemoveChunk(chunk.ChunkIndex, out List<Chunk> neighbourChunks);
                                for (int j = 0; j < neighbourChunks.Count; j++)
                                {
                                    ResetNeighbours(overrideChunks, neighbourChunks[j], 1);
                                }
                            }   
                        }
                    }
                    else if (isBuildable)
                    {
                        positions.Add(pos);
                        if (ChunkIndexToChunks.TryGetValue(buildable.ChunkIndex, out List<int3> list))
                        {
                            list.Add(index);
                        }
                        else
                        {
                            ChunkIndexToChunks.Add(buildable.ChunkIndex, new List<int3> { index });
                        }
                    }
                }
            }
        }

        private async UniTask IterativeFailChecks(HashSet<Chunk> chunksToCollapse)
        {
            while (CheckFailed(chunksToCollapse))
            {
                HashSet<Chunk> neighbours = new HashSet<Chunk>();
                foreach (Chunk overrideChunk in chunksToCollapse)
                {
                    for (int i = 0; i < overrideChunk.AdjacentChunks.Length; i++)
                    {
                        if (overrideChunk.AdjacentChunks[i] is not Chunk || chunksToCollapse.Contains(overrideChunk)) continue;

                        neighbours.Add(overrideChunk);
                    }
                }

                chunksToCollapse.AddRange(neighbours);

                foreach (Chunk chunk in chunksToCollapse)
                {
                    chunk.Clear(waveFunction.GameObjectPool);
                    waveFunction.LoadCells(chunk, defaultPrototypeInfoData);
                }

                await Run();
                await UniTask.Yield();
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
                    if (cell.PossiblePrototypes[0].MeshRot.MeshIndex != -1)
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

        private void ResetNeighbours(HashSet<Chunk> overrideChunks, Chunk neighbourChunk, int depth)
        {
            for (int i = 0; i < neighbourChunk.AdjacentChunks.Length; i++)
            {
                if (neighbourChunk.AdjacentChunks[i] == null) continue;

                if (overrideChunks.Add(neighbourChunk.AdjacentChunks[i] as Chunk))
                {
                    neighbourChunk.AdjacentChunks[i].Clear(waveFunction.GameObjectPool);
                    if (depth > 0)
                    {
                        ResetNeighbours(overrideChunks, neighbourChunk.AdjacentChunks[i] as Chunk, depth - 1);
                    }
                }
            }
        }

        public async UniTaskVoid RemoveChunks(List<ChunkIndex> chunkIndexes)
        {
            await UniTask.WaitWhile(() => IsGenerating);

            HashSet<Chunk> neighbours = new HashSet<Chunk>();
            HashSet<int3> killIndexes = new HashSet<int3>();
            for (int i = 0; i < chunkIndexes.Count; i++)
            {
                if (!ChunkIndexToChunks.TryGetValue(chunkIndexes[i], out List<int3> indexes))
                {
                    continue;
                }
                    
                for (int j = indexes.Count - 1; j >= 0; j--)
                {
                    if (!waveFunction.Chunks.ContainsKey(indexes[j]))
                    {
                        indexes.RemoveAt(j);
                        continue;
                    }
                    
                    killIndexes.Add(indexes[j]);
                    ResetNeighbours(neighbours, waveFunction.Chunks[indexes[j]], 1);
                }
                
                ChunkIndexToChunks.Remove(chunkIndexes[i]);
            }

            foreach (int3 index in killIndexes)
            {
                neighbours.Remove(waveFunction.Chunks[index]);
                waveFunction.RemoveChunk(index);
            }
            
            foreach (Chunk neighbour in neighbours)
            {
                waveFunction.LoadCells(neighbour, neighbour.PrototypeInfoData);
            }
            
            waveFunction.Propagate();
            
            await Run();
            await UniTask.Yield();

            IterativeFailChecks(neighbours).Forget(Debug.LogError);
        }

        public async UniTask Run()
        {
            await UniTask.WaitWhile(() => IsGenerating);

            IsGenerating = true;
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

            IsGenerating = false;
        }

        #region Debug

#if UNITY_EDITOR
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
                    Gizmos.DrawWireCube(pos, new Vector3(waveFunction.CellSize.x, waveFunction.CellSize.y, waveFunction.CellSize.z) * 0.75f);
                }
            }
        }
#endif
        #endregion
    }
}
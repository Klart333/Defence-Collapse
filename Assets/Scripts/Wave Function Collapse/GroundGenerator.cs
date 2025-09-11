using System.Collections.Generic;
using Debug = UnityEngine.Debug;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using System.Diagnostics;
using Unity.Mathematics;
using Gameplay.Event;
using UnityEngine;
using System;
using Enemy;

namespace WaveFunctionCollapse
{
    public class GroundGenerator : MonoBehaviour, IChunkWaveFunction<Chunk>
    {
        public event Action<Chunk> OnChunkGenerated;
        public event Action<ChunkIndex> OnCellCollapsed;
        public event Action OnGenerationFinished;

        [Title("Wave Function")]
        [SerializeField]
        private ChunkWaveFunction<Chunk> waveFunction;
        
        [SerializeField]
        private Vector3Int chunkSize;
        
        [SerializeField]
        private PrototypeInfoData defaultPrototypeInfoData;

        [SerializeField]
        private int maximumSize = 1;
        
        [Title("Settings")]
        [SerializeField]
        private int awaitEveryFrame = 1;

        [SerializeField]
        private int awaitTimeMs;

        [SerializeField]
        private float maxMillisecondsPerFrame = 4;

        [SerializeField]
        private bool shouldCombine = true;

        [Title("Enemy Spawning")]
        [SerializeField]
        private EnemySpawnHandler enemySpawnHandler; 
        
        [Title("Debug")]
        [SerializeField]
        private bool shouldRun = true;
        
        [SerializeField]
        private bool compilePrototypeMeshes = true;
        
        private Dictionary<int3, HashSet<int3>> generatedChunkIndexes = new Dictionary<int3, HashSet<int3>>();
        private HashSet<int3> generatedChunks = new HashSet<int3>();
        private HashSet<int3> spawnedChunks = new HashSet<int3>();
        private List<int3> chunksToCombine = new List<int3>();
        
        private GroundAnimator groundAnimator;
        private MeshCombiner meshCombiner;

        public bool IsGenerating { get; private set; }
        
        public Vector3 ChunkScale => new Vector3(chunkSize.x * ChunkWaveFunction.CellSize.x, chunkSize.y * ChunkWaveFunction.CellSize.y, chunkSize.z * ChunkWaveFunction.CellSize.z);
        public int3 ChunkSize => new int3(chunkSize.x, chunkSize.y, chunkSize.z);
        public ChunkWaveFunction<Chunk> ChunkWaveFunction => waveFunction;
        public HashSet<int3> GeneratedChunks => generatedChunks;
        
#if UNITY_EDITOR
        private async UniTaskVoid Start()
        {
            if (compilePrototypeMeshes)
            {
                //waveFunction.ProtoypeMeshes.CompilePrototypes();
                waveFunction.ProtoypeMeshes.CompileData();

                await UniTask.Delay(TimeSpan.FromSeconds(0.1f));
            }
#else
        private void Start()
        {
#endif
            groundAnimator = GetComponent<GroundAnimator>();
            meshCombiner = GetComponent<MeshCombiner>();
            
            if (!waveFunction.Load(this)) return;
            waveFunction.ParentTransform = transform;

            if (shouldRun) SetupGround().Forget();
        }

        private async UniTaskVoid SetupGround()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            await PreGenerate();
            stopwatch.Stop();
            Debug.Log("Generated In: " + stopwatch.Elapsed.TotalMilliseconds + "ms");
            
            LoadChunk(new int3(0, 0, 0));
        }

        private async UniTask PreGenerate()
        {
            IsGenerating = true;
            
            for (int x = -maximumSize; x <= maximumSize; x++)
            for (int z = -maximumSize; z <= maximumSize; z++)
            {
                int3 chunkIndex = new int3(x, 0, z);
                if (waveFunction.Chunks.ContainsKey(chunkIndex)) continue;
                
                waveFunction.LoadChunk(chunkIndex, chunkSize, defaultPrototypeInfoData, false);
            }
            
            waveFunction.Propagate();

            int cellCount = 0;
            foreach (Chunk chunk in waveFunction.Chunks.Values)
            {
                cellCount += chunk.Width * chunk.Height * chunk.Depth;
            }
            await Run(cellCount, () => waveFunction.Iterate(false));

            IsGenerating = false;
        }
        
        private async Task Run(int cellCountToCollapse, Action Iterate)
        {
            Stopwatch watch = Stopwatch.StartNew();
            int frameCount = 0;
            int collapseCount = 0;
            while (collapseCount < cellCountToCollapse)
            {
                watch.Start();
                Iterate.Invoke();
                watch.Stop();
                collapseCount++;
                
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

        public void LoadChunk(int3 chunkIndex)
        {
            IsGenerating = true;
            Chunk chunk = waveFunction.Chunks[chunkIndex];
            
            DisplayAdjacentChunks(chunk);
            DisplayChunk(chunk);

            groundAnimator.OnAnimationFinished += OnGroundAnimatorFinished;

            OnChunkGenerated?.Invoke(chunk);
            chunksToCombine.Add(chunkIndex);
            generatedChunks.Add(chunkIndex);
        }

        private void DisplayChunk(Chunk chunk)
        {
            HashSet<int3> builtCells = generatedChunkIndexes.GetValueOrDefault(chunk.ChunkIndex, new HashSet<int3>());
            List<ChunkIndex> cells = new List<ChunkIndex>();
            for (int x = 0; x < chunk.Width; x++)
            for (int y = 0; y < chunk.Height; y++)
            for (int z = 0; z < chunk.Depth; z++)
            {
                int3 cellIndex = new int3(x, y, z);
                if (builtCells.Contains(cellIndex)) continue;

                cells.Add(new ChunkIndex(chunk.ChunkIndex, cellIndex));
            }
            
            DisplayCells(cells);
        }
        
        private void DisplayCells(IEnumerable<ChunkIndex> cells)
        {
            foreach (ChunkIndex chunkIndex in cells)
            {
                waveFunction.SetCell(chunkIndex, waveFunction[chunkIndex].PossiblePrototypes[0]);
                OnCellCollapsed?.Invoke(chunkIndex);
                
                if (generatedChunkIndexes.TryGetValue(chunkIndex.Index, out HashSet<int3> list)) list.Add(chunkIndex.CellIndex);
                else generatedChunkIndexes.Add(chunkIndex.Index, new HashSet<int3> { chunkIndex.CellIndex });
            }
        }
        
        private void DisplayAdjacentChunks(Chunk chunk)
        {
            List<Chunk> adjacentChunks = new List<Chunk>();
            List<MultiDirection> directions = new List<MultiDirection>();
            for (int x = -1; x <= 1; x++)
            for (int z = -1; z <= 1; z++)
            {
                if (x == 0 && z == 0) continue;
                
                int3 chunkIndex = new int3(chunk.ChunkIndex.x + x, 0, chunk.ChunkIndex.z + z);
                if (!waveFunction.Chunks.TryGetValue(chunkIndex, out Chunk adjacent)) continue;

                MultiDirection dir = DirectionUtility.Int2ToMultiDirection(new int2(-x, -z));
                adjacentChunks.Add(adjacent);
                directions.Add(dir);
            }
            
            for (int i = 0; i < adjacentChunks.Count; i++)
            {
                List<ChunkIndex> cells = GetSideIndexes(adjacentChunks[i], directions[i]);
                if (cells.Count == 0) continue;
                
                DisplayCells(cells);
                enemySpawnHandler.AddSpawnPoints(cells);
                chunksToCombine.Add(adjacentChunks[i].ChunkIndex);

                if (spawnedChunks.Contains(adjacentChunks[i].ChunkIndex))
                {
                    continue;
                }

                generatedChunks.Add(adjacentChunks[i].ChunkIndex);
                Events.OnGroundChunkGenerated?.Invoke(adjacentChunks[i]);
            }
        }

        private List<ChunkIndex> GetSideIndexes(Chunk chunk, MultiDirection direction)
        {
            HashSet<int3> builtCells = generatedChunkIndexes.GetValueOrDefault(chunk.ChunkIndex, new HashSet<int3>());
            List<ChunkIndex> cells = new List<ChunkIndex>();
            int length = chunk.Width;

            // Extract all active flags
            List<MultiDirection> activeDirections = new List<MultiDirection>();
            foreach (MultiDirection dir in Enum.GetValues(typeof(MultiDirection)))
            {
                if (direction.HasFlag(dir))
                {
                    activeDirections.Add(dir);
                }   
            }

            // Case 1: Corner (2+ flags) -> single overlap index
            if (activeDirections.Count == 2)
            {
                int x = direction.HasFlag(MultiDirection.Right)   ? length - 1 : 0;
                int z = direction.HasFlag(MultiDirection.Forward) ? length - 1 : 0;

                int3 index = new(x, 0, z);
                if (!builtCells.Contains(index))
                {
                    cells.Add(new ChunkIndex(chunk.ChunkIndex, index));
                }
                return cells;
            }

            // Case 2: Single side -> add the whole edge
            for (int i = 0; i < length; i++)
            {
                int3 targetIndex = direction switch
                {
                    MultiDirection.Right => new int3(length - 1, 0, i),
                    MultiDirection.Left => new int3(0, 0, i),
                    MultiDirection.Forward => new int3(i, 0, length - 1),
                    MultiDirection.Backward => new int3(i, 0, 0),
                    _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
                };

                if (builtCells.Contains(targetIndex)) continue;
                
                cells.Add(new ChunkIndex(chunk.ChunkIndex, targetIndex));
            }

            return cells;
        }
        
        private void OnGroundAnimatorFinished()
        {
            groundAnimator.OnAnimationFinished -= OnGroundAnimatorFinished;

            if (shouldCombine)
            {
                foreach (int3 chunkIndex in chunksToCombine)
                {
                    CombineChunk(waveFunction.Chunks[chunkIndex]);
                }
                
                chunksToCombine.Clear();
            }
            
            IsGenerating = false;
            OnGenerationFinished?.Invoke();
        }
        
        private void CombineChunk(Chunk chunk)
        {
            CombineMeshes(chunk.ChunkIndex, ChunkWaveFunction.ChunkParents[chunk.ChunkIndex]);
            chunk.ClearSpawnedMeshes(waveFunction.GameObjectPool);
        }
        
        private void CombineMeshes(int3 chunkIndex, Transform chunkParent)
        {
            meshCombiner.CombineMeshes(chunkParent, out GameObject spawnedMesh);
            spawnedChunks.Add(chunkIndex);
        }
    }
}
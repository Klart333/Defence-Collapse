using System.Collections.Generic;
using Debug = UnityEngine.Debug;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using System.Diagnostics;
using Unity.Mathematics;
using Unity.Entities;
using UnityEngine;
using System;
using Enemy;
using Gameplay.Event;

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
        
        private Dictionary<int3, GameObject> spawnedChunks = new Dictionary<int3, GameObject>();
        private HashSet<int3> generatedChunks = new HashSet<int3>();
        
        private GroundAnimator groundAnimator;
        private MeshCombiner meshCombiner;

        public bool IsGenerating { get; private set; }
        
        public Vector3 ChunkScale => new Vector3(chunkSize.x * ChunkWaveFunction.CellSize.x, chunkSize.y * ChunkWaveFunction.CellSize.y, chunkSize.z * ChunkWaveFunction.CellSize.z);
        public int3 ChunkSize => new int3(chunkSize.x, chunkSize.y, chunkSize.z);
        public ChunkWaveFunction<Chunk> ChunkWaveFunction => waveFunction;
        
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

            if (spawnedChunks.ContainsKey(chunkIndex))
            {
                chunk.InvokeOnCleared();
            }
            
            DisplayAdjacentChunks(chunk);
            DisplayChunk(chunk);

            groundAnimator.OnAnimationFinished += OnGroundAnimatorFinished;

            OnChunkGenerated?.Invoke(chunk);
            generatedChunks.Add(chunk.ChunkIndex);
            //RemoveUnreferencedChunks();
        }

        private void DisplayChunk(Chunk chunk)
        {
            List<ChunkIndex> cells = new List<ChunkIndex>();
            for (int x = 0; x < chunk.Width; x++)
            for (int y = 0; y < chunk.Height; y++)
            for (int z = 0; z < chunk.Depth; z++)
            {
                cells.Add(new ChunkIndex(chunk.ChunkIndex, new int3(x, y, z)));
            }
            
            DisplayCells(cells);
        }
        
        private void DisplayCells(IEnumerable<ChunkIndex> cells)
        {
            foreach (ChunkIndex chunkIndex in cells)
            {
                waveFunction.SetCell(chunkIndex, waveFunction[chunkIndex].PossiblePrototypes[0]);
                OnCellCollapsed?.Invoke(chunkIndex);
            }
        }
        
        private void DisplayAdjacentChunks(Chunk chunk)
        {
            List<Chunk> adjacentChunks = new List<Chunk>();
            List<Direction> directions = new List<Direction>();
            for (int x = -1; x <= 1; x++)
            for (int z = -1; z <= 1; z++)
            {
                if (x == 0 && z == 0 || x != 0 && z != 0) continue;
                
                int3 chunkIndex = new int3(chunk.ChunkIndex.x + x, 0, chunk.ChunkIndex.z + z);
                if (spawnedChunks.ContainsKey(chunkIndex) || !waveFunction.Chunks.TryGetValue(chunkIndex, out Chunk adjacent)) continue;

                Direction dir = DirectionUtility.Int2ToDirection(new int2(-x, -z));
                adjacentChunks.Add(adjacent);
                directions.Add(dir);
            }
            
            for (int i = 0; i < adjacentChunks.Count; i++)
            {
                List<ChunkIndex> cells = GetSideIndexes(adjacentChunks[i], directions[i]);
                
                DisplayCells(cells);
                generatedChunks.Add(adjacentChunks[i].ChunkIndex);
                Events.OnGroundChunkGenerated?.Invoke(adjacentChunks[i]);
                
                enemySpawnHandler.AddSpawnPoints(cells);
            }
        }

        private List<ChunkIndex> GetSideIndexes(Chunk chunk, Direction direction)
        {
            List<ChunkIndex> cells = new();
            int length = chunk.Width;

            for (int i = 0; i < length; i++)
            {
                int3 adjacent = direction switch
                {
                    Direction.Right    => new int3(length - 1, 0, i         ),          
                    Direction.Left     => new int3(0,          0, i         ),          
                    Direction.Forward  => new int3(i,          0, length - 1),
                    Direction.Backward => new int3(i,          0, 0         ),          
                    _ => default
                };
                
                cells.Add(new ChunkIndex(chunk.ChunkIndex, adjacent));
            }

            return cells;
        }
        
        private void OnGroundAnimatorFinished()
        {
            groundAnimator.OnAnimationFinished -= OnGroundAnimatorFinished;

            if (shouldCombine)
            {
                foreach (int3 chunkIndex in generatedChunks)
                {
                    if (spawnedChunks.ContainsKey(chunkIndex))
                    {
                        continue;
                    }
                    
                    CombineChunk(waveFunction.Chunks[chunkIndex]);
                }
            }
            
            IsGenerating = false;
            OnGenerationFinished?.Invoke();
        }
        
        private void CombineChunk(Chunk chunk)
        {
            CombineMeshes(chunk.ChunkIndex, ChunkWaveFunction.ChunkParents[chunk.ChunkIndex]);
            chunk.ClearSpawnedMeshes(waveFunction.GameObjectPool);
            chunk.OnCleared += ChunkOnOnCleared;
            
            void ChunkOnOnCleared()
            {
                chunk.OnCleared -= ChunkOnOnCleared;
                
                spawnedChunks[chunk.ChunkIndex].SetActive(false);
                spawnedChunks.Remove(chunk.ChunkIndex);
            }
        }
        
        private void CombineMeshes(int3 chunkIndex, Transform chunkParent)
        {
            meshCombiner.CombineMeshes(chunkParent, out GameObject spawnedMesh);
            spawnedChunks.Add(chunkIndex, spawnedMesh);
        }
    }
}
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using System.Diagnostics;
using Unity.Mathematics;
using UnityEngine;
using System;
using Enemy;

namespace WaveFunctionCollapse
{
    public class GroundGenerator : MonoBehaviour, IChunkWaveFunction<Chunk>
    {
        public event Action<Chunk> OnChunkGenerated;
        public event Action<ChunkIndex> OnCellCollapsed;

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
        
        [SerializeField]
        private int fullTreeIndex = 1;
        
        [SerializeField]
        private int enemyGateIndex = 1;

        [Title("Crystal")]
        [SerializeField]
        private int[] crystalCornerIndexes;
        
        [Title("Debug")]
        [SerializeField]
        private bool shouldRun = true;
        
        [SerializeField]
        private bool compilePrototypeMeshes = true;
        
        private readonly Dictionary<int3, GameObject> spawnedChunks = new Dictionary<int3, GameObject>();
        private readonly HashSet<int3> generatedChunks = new HashSet<int3>();
        
        private MeshCombiner meshCombiner;

        public bool IsGenerating { get; private set; }
        
        public Vector3 ChunkScale => new Vector3(chunkSize.x * ChunkWaveFunction.CellSize.x, chunkSize.y * ChunkWaveFunction.CellSize.y, chunkSize.z * ChunkWaveFunction.CellSize.z);
        public PrototypeInfoData DefaultPrototypeInfoData => defaultPrototypeInfoData;
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
            meshCombiner = GetComponent<MeshCombiner>();

            if (!waveFunction.Load(this)) return;
            waveFunction.ParentTransform = transform;

            if (shouldRun) SetupGround().Forget();
        }

        private async UniTaskVoid SetupGround()
        {
            await PreGenerate();
            
            LoadChunk(waveFunction.Chunks[new int3(0, 0, 0)]).Forget();
        }

        public async UniTask PreGenerate()
        {
            for (int x = -maximumSize; x <= maximumSize; x++)
            for (int z = -maximumSize; z <= maximumSize; z++)
            {
                int3 chunkIndex = new int3(x, 0, z);
                if (waveFunction.Chunks.ContainsKey(chunkIndex)) continue;
                
                waveFunction.LoadChunk(chunkIndex, chunkSize, defaultPrototypeInfoData, false);
            }
            
            waveFunction.Propagate();
            
            await Run(waveFunction.AllCollapsed, () => waveFunction.Iterate(false));
        }

        public async UniTaskVoid LoadChunk(Chunk chunk)
        {
            IsGenerating = true;
            
            await DisplayAdjacentChunks(chunk);

            waveFunction.Propagate();
            await Run(() => chunk.AllCollapsed, () => Iterate(chunk, false));

            if (shouldCombine)
            {
                CombineChunk(chunk, false).Forget();
            }

            OnChunkGenerated?.Invoke(chunk);
            generatedChunks.Add(chunk.ChunkIndex);
            RemoveUnreferencedChunks();

            IsGenerating = false;
        }

        private void RemoveUnreferencedChunks()
        {
            List<int3> unreferencedChunks = new List<int3>();
            foreach (KeyValuePair<int3, Chunk> kvp in ChunkWaveFunction.Chunks)
            {
                if (!generatedChunks.Contains(kvp.Key)) continue;
                bool valid = true;
                for (int i = 0; valid && i < WaveFunctionUtility.NeighbourDirections.Length; i++)
                {
                    int3 index = new int3(kvp.Key.x + WaveFunctionUtility.NeighbourDirections[i].x, 0, kvp.Key.z + WaveFunctionUtility.NeighbourDirections[i].y);
                    if (!generatedChunks.Contains(index))
                    {
                        valid = false;
                    }
                }

                if (valid)
                {
                    unreferencedChunks.Add(kvp.Key);
                }
            }

            for (int i = 0; i < unreferencedChunks.Count; i++)
            {
                ChunkWaveFunction.RemoveChunk(unreferencedChunks[i], out _);
            }
        }

        private async UniTaskVoid CombineChunk(Chunk chunk, bool subToEvent = true)
        {
            await UniTask.Delay(1000);
            CombineMeshes(chunk.ChunkIndex, ChunkWaveFunction.ChunkParents[chunk.ChunkIndex]);
            chunk.ClearSpawnedMeshes(waveFunction.GameObjectPool);

            if (subToEvent)
            {
                chunk.OnCleared += ChunkOnOnCleared;
            }

            void ChunkOnOnCleared()
            {
                chunk.OnCleared -= ChunkOnOnCleared;
                
                spawnedChunks[chunk.ChunkIndex].SetActive(false);
                spawnedChunks.Remove(chunk.ChunkIndex);
            }
        }

        private async Task Run(Func<bool> AllCollapsed, Action Iterate)
        {
            Stopwatch watch = Stopwatch.StartNew();
            int frameCount = 0;
            while (!AllCollapsed())
            {
                watch.Start();
                Iterate.Invoke();
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
        
        public ChunkIndex Iterate(Chunk chunk, bool shouldSpawn)
        {
            ChunkIndex index = waveFunction.GetLowestEntropyIndex(chunk);
            PrototypeData chosenPrototype = waveFunction.Collapse(waveFunction[index]);
            waveFunction.SetCell(index, chosenPrototype, shouldSpawn);

            waveFunction.Propagate();
            return index;
        }
        
        public ChunkIndex Iterate(List<ChunkIndex> cells, bool shouldSpawn)
        {
            ChunkIndex index = waveFunction.GetLowestEntropyIndex(cells);
            PrototypeData chosenPrototype = waveFunction.Collapse(waveFunction[index]);
            waveFunction.SetCell(index, chosenPrototype, shouldSpawn);

            waveFunction.Propagate();
            return index;
        }
        
        private async UniTask DisplayAdjacentChunks(Chunk chunk)
        {
            List<Chunk> adjacentChunks = new List<Chunk>();
            List<Direction> directions = new List<Direction>();
            for (int x = -1; x <= 1; x++)
            for (int z = -1; z <= 1; z++)
            {
                if (x == 0 && z == 0 || x != 0 && z != 0) continue;
                
                int3 chunkIndex = new int3(chunk.ChunkIndex.x + x, 0, chunk.ChunkIndex.z + z);
                if (waveFunction.Chunks.ContainsKey(chunkIndex)) continue;
                
                Chunk adjacent = waveFunction.LoadChunk(chunkIndex, chunkSize, defaultPrototypeInfoData, false);
                Direction dir = DirectionUtility.Int2ToDirection(new int2(-x, -z));
                adjacentChunks.Add(adjacent);
                directions.Add(dir);
            }

            SetSpawnPoints();

            for (int i = 0; i < adjacentChunks.Count; i++)
            {
                List<ChunkIndex> cells = GetSideIndexes(adjacentChunks[i], directions[i]);
                
                await Run(() => adjacentChunks[i].AllCollapsed, () => Iterate(adjacentChunks[i], true));
                Events.OnGroundChunkGenerated?.Invoke(adjacentChunks[i]);
                if (shouldCombine)
                {
                    CombineChunk(adjacentChunks[i]).Forget();
                }
            }

            void SetSpawnPoints()
            {
                int spawned = 0;
                int count = adjacentChunks.Count;
                for (int i = 0; i < count; i++)
                {
                    if (directions[i] == Direction.None) continue;

                    int3 chunkIndex = adjacentChunks[i].ChunkIndex;
                    if (!enemySpawnHandler.ShouldSetSpawnPoint(chunkIndex, out int difficulty)
                        && !enemySpawnHandler.ShouldForceSpawnPoint(i, count - 1, spawned, difficulty)) continue;
                
                    if (enemySpawnHandler.GetMaxSpawns(difficulty) <= spawned) break;
                    
                    spawned++;  
                    SetEnemySpawn(adjacentChunks[i], directions[i], difficulty);
                }
            }
        }

        private List<ChunkIndex> GetSideIndexes(Chunk chunk, Direction direction)
        {
            if (direction == Direction.None) return new List<ChunkIndex>();
            
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

        private void SetEnemySpawn(Chunk chunk, Direction direction, int difficulty)
        {
            float middle = (chunk.Depth - 1) / 2.0f;

            bool isHorizontal = direction is Direction.Right or Direction.Left;
            int edge = isHorizontal 
                ? direction == Direction.Right ? chunk.Width - 1 : 0
                : direction == Direction.Forward ? chunk.Depth - 1 : 0;

            int end = isHorizontal ? chunk.Depth : chunk.Width;
            for (int i = 1; i < end - 1; i++)
            {
                int x = isHorizontal ? edge : i;
                int z = isHorizontal ? i : edge;

                List<PrototypeData> prots = new List<PrototypeData>
                {
                    defaultPrototypeInfoData.Prototypes[isHorizontal 
                        ? enemyGateIndex + 1 + (i - 1) * 2
                        : enemyGateIndex + 2 - (i - 1) * 2
                    ]
                };

                chunk.Cells[x, 0, z] = new Cell(false, chunk.Cells[x, 0, z].Position, prots);
                ChunkIndex index = new ChunkIndex(chunk.ChunkIndex, new int3(x, 0, z));
                waveFunction.CellStack.Push(index);
            }

            Vector3 pos = isHorizontal
                ? (chunk.Cells[edge, 0, Mathf.FloorToInt(middle)].Position + chunk.Cells[edge, 0, Mathf.CeilToInt(middle)].Position) / 2.0f
                : (chunk.Cells[Mathf.FloorToInt(middle), 0, edge].Position + chunk.Cells[Mathf.CeilToInt(middle), 0, edge].Position) / 2.0f;
            enemySpawnHandler.SetEnemySpawn(pos - (Vector3)DirectionUtility.DirectionToInt2(direction).XyZ(0.0f), chunk.ChunkIndex, difficulty);
        }
        
        private void CombineMeshes(int3 chunkIndex, Transform chunkParent)
        {
            meshCombiner.CombineMeshes(chunkParent, out GameObject spawnedMesh);
            spawnedChunks.Add(chunkIndex, spawnedMesh);
        }
    }
}
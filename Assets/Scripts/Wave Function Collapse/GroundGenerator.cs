using MeshCollider = Unity.Physics.MeshCollider;
using Material = Unity.Physics.Material;
using Collider = Unity.Physics.Collider;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using UnityEngine.Assertions;
using Sirenix.OdinInspector;
using System.Diagnostics;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.Entities;
using Unity.Physics;
using UnityEngine;
using System;
using Chunks;
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
        
        [Title("Settings")]
        [SerializeField]
        private int awaitEveryFrame = 1;

        [SerializeField]
        private int awaitTimeMs;

        [SerializeField]
        private float maxMillisecondsPerFrame = 4;

        [SerializeField]
        private bool shouldCombine = true;

        [Title("References")]
        [SerializeField]
        private ChunkMaskHandler chunkMaskHandler;

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
        private readonly Dictionary<int3, Entity> spawnedChunkColliders = new Dictionary<int3, Entity>();
        private readonly HashSet<int3> generatedChunks = new HashSet<int3>();

        private NativeList<BlobAssetReference<Collider>> spawnedColliders;
        
        private EntityManager entityManager;

        public bool IsGenerating { get; private set; }
        public ChunkWaveFunction<Chunk> ChunkWaveFunction => waveFunction;
        public PrototypeInfoData DefaultPrototypeInfoData => defaultPrototypeInfoData;
        public Vector3 ChunkScale => new Vector3(chunkSize.x * ChunkWaveFunction.CellSize.x, chunkSize.y * ChunkWaveFunction.CellSize.y, chunkSize.z * ChunkWaveFunction.CellSize.z);
        public Vector3Int ChunkSize => chunkSize;
        
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
            spawnedColliders = new NativeList<BlobAssetReference<Collider>>(Allocator.Persistent);
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            if (!waveFunction.Load(this))
            {
                return;
            }
            waveFunction.ParentTransform = transform;
            
            Chunk chunk = waveFunction.LoadChunk(transform.position, chunkSize, defaultPrototypeInfoData, false);
            //SetCrystalInCenter(chunk);
            
            if (shouldRun)
                LoadChunk(chunk).Forget();
        }

        private void OnDisable()
        {
            if (spawnedColliders.IsCreated)
            {
                foreach (BlobAssetReference<Collider> spawnedCollider in spawnedColliders)
                {
                    if (spawnedCollider.IsCreated)
                    {
                        spawnedCollider.Dispose();
                    }
                }
            
                spawnedColliders.Dispose();
            }
           
        }

        public async UniTaskVoid LoadChunk(Chunk chunk)
        {
            IsGenerating = true;
            if (chunkMaskHandler.isActiveAndEnabled) chunkMaskHandler.RemoveMask(chunk);
            
            await LoadAdjacentChunks(chunk);

            waveFunction.Propagate();
            await Run(chunk);

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

                if (spawnedChunkColliders.ContainsKey(chunk.ChunkIndex))
                {
                    entityManager.DestroyEntity(spawnedChunkColliders[chunk.ChunkIndex]);
                    spawnedChunkColliders.Remove(chunk.ChunkIndex);   
                }
            }
        }

        private async Task Run(Chunk chunk)
        {
            Stopwatch watch = Stopwatch.StartNew();
            int frameCount = 0;
            while (!chunk.AllCollapsed)
            {
                watch.Start();
                ChunkIndex index = waveFunction.Iterate(chunk);
                OnCellCollapsed?.Invoke(index);
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
        
        private async Task Run(List<ChunkIndex> cells)
        {
            Stopwatch watch = Stopwatch.StartNew();
            int frameCount = 0;
            while (!cells.TrueForAll((x) => ChunkWaveFunction[x].Collapsed))
            {
                watch.Start();
                ChunkIndex index = waveFunction.Iterate(cells);
                OnCellCollapsed?.Invoke(index);
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

        private async UniTask LoadAdjacentChunks(Chunk chunk)
        {
            List<Chunk> adjacentChunks = new List<Chunk>();
            List<Direction> directions = new List<Direction>();
            for (int x = -1; x <= 1; x++)
            for (int z = -1; z <= 1; z++)
            {
                if (x == 0 && z == 0 || x != 0 && z != 0)
                {
                    continue;
                }
                
                int3 chunkIndex = new int3(chunk.ChunkIndex.x + x, 0, chunk.ChunkIndex.z + z);
                if (waveFunction.Chunks.ContainsKey(chunkIndex))
                {
                    continue;
                }
                
                Chunk adjacent = waveFunction.LoadChunk(chunkIndex, chunkSize, defaultPrototypeInfoData, false);
                if (chunkMaskHandler.isActiveAndEnabled) chunkMaskHandler.CreateMask(adjacent, Utility.Math.IntToAdjacency(new int2(-x, -z)));
                adjacentChunks.Add(adjacent);
                directions.Add(DirectionUtility.Int2ToDirection(new int2(-x, -z)));
            }

            SetSpawnPoints();

            waveFunction.Propagate();
            for (int i = 0; i < adjacentChunks.Count; i++)
            {
                List<ChunkIndex> cells = GetSideIndexes(adjacentChunks[i], directions[i]);
                
                await Run(cells);
                Events.OnGroundChunkGenerated?.Invoke(adjacentChunks[i]);
                if (shouldCombine)
                {
                    CombineChunk(adjacentChunks[i]).Forget();
                }
            }

            void SetSpawnPoints()
            {
                int spawned = 0;
                int chunkCount = adjacentChunks.Count;
                for (int i = 0; i < chunkCount; i++)
                {
                    int3 chunkIndex = adjacentChunks[i].ChunkIndex;
                    if (!enemySpawnHandler.ShouldSetSpawnPoint(chunkIndex, out int difficulty)
                        && !enemySpawnHandler.ShouldForceSpawnPoint(i, chunkCount - 1, spawned, difficulty)) continue;
                
                    if (enemySpawnHandler.GetMaxSpawns(difficulty) <= spawned) break;
                    
                    spawned++;  
                    SetEnemySpawn(adjacentChunks[i], directions[i], difficulty);
                }
            }
        }

        private List<ChunkIndex> GetSideIndexes(Chunk chunk, Direction direction)
        {
            List<ChunkIndex> cells = new();
            int length = chunk.Width;

            for (int i = 0; i < length; i++)
            {
                (int3 a, int3 b) = direction switch
                {
                    Direction.Right    => (new int3(length - 1, 0, i),          new int3(length - 2, 0, i)),
                    Direction.Left     => (new int3(0,          0, i),          new int3(1,          0, i)),
                    Direction.Forward  => (new int3(i,          0, length - 1), new int3(i,          0, length - 2)),
                    Direction.Backward => (new int3(i,          0, 0),          new int3(i,          0, 1)),
                    _ => default
                };
                
                cells.Add(new ChunkIndex(chunk.ChunkIndex, a));
                //cells.Add(new ChunkIndex(chunk.ChunkIndex, b));
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

        private void SetCrystalInCenter(Chunk chunk)
        {
            float middle = (chunk.Depth - 1) / 2.0f;
            int start = Mathf.FloorToInt(middle);
            int end = Mathf.CeilToInt(middle);
            Assert.AreNotEqual(start, end);

            int index = 0;
            for (int x = start; x <= end; x++)
            {
                for (int z = start; z <= end; z++)
                {
                    List<PrototypeData> prots = new List<PrototypeData>
                    {
                        defaultPrototypeInfoData.Prototypes[crystalCornerIndexes[index++]]
                    };
                    
                    chunk.Cells[x, 0, z] = new Cell(false, chunk.Cells[x, 0, z].Position, prots);
                    ChunkIndex chunkIndex = new ChunkIndex(chunk.ChunkIndex, new int3(x, 0, z));
                    waveFunction.CellStack.Push(chunkIndex);
                }
            }    
        }
        
        private void CombineMeshes(int3 chunkIndex, Transform chunkParent)
        {
            Mesh mesh = GetComponent<MeshCombiner>().CombineMeshes(chunkParent, out GameObject spawnedMesh);
            spawnedChunks.Add(chunkIndex, spawnedMesh);
            return;
            
            BlobAssetReference<Collider> blobCollider = MeshCollider.Create(mesh, new CollisionFilter
            {
                BelongsTo = 6,
                CollidesWith = 6,
                GroupIndex = 0,
            }, Material.Default);

            ComponentType[] componentTypes = new ComponentType[4]
            {
                typeof(LocalTransform),
                typeof(LocalToWorld),
                typeof(PhysicsCollider),
                typeof(PhysicsWorldIndex),
            };

            Entity entity = entityManager.CreateEntity(componentTypes);
            entityManager.SetComponentData(entity, new LocalToWorld
            {
                Value = gameObject.transform.localToWorldMatrix,
            });
            entityManager.SetComponentData(entity, new LocalTransform()
            {
                Position = transform.localPosition,
                Rotation = transform.localRotation,
                Scale = transform.localScale.x,
            });
            entityManager.SetComponentData(entity, new PhysicsCollider
            {
                Value = blobCollider,
            });
            
            spawnedChunkColliders.Add(chunkIndex, entity);
            
            spawnedColliders.Add(blobCollider);
        }
    }
}
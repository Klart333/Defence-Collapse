using MeshCollider = Unity.Physics.MeshCollider;
using Material = Unity.Physics.Material;
using Collider = Unity.Physics.Collider;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using System.Diagnostics;
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
        public event Action<Chunk> OnLockedChunkGenerated;
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
        private int chillTimeMs;

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
        
        [Title("Debug")]
        [SerializeField]
        private bool shouldRun = true;
        
        private BlobAssetReference<Collider> blobCollider;

        public ChunkWaveFunction<Chunk> ChunkWaveFunction => waveFunction;
        public Vector3 ChunkScale => new Vector3(chunkSize.x * ChunkWaveFunction.GridScale.x, chunkSize.y * ChunkWaveFunction.GridScale.y, chunkSize.z * ChunkWaveFunction.GridScale.z);
        
        private void Start()
        {
            if (!waveFunction.Load(this))
            {
                return;
            }
            waveFunction.ParentTransform = transform;
            
            Chunk chunk = waveFunction.LoadChunk(transform.position, chunkSize, defaultPrototypeInfoData, false);

            if (shouldRun)
                LoadChunk(chunk).Forget(Debug.LogError);
        }

        private void OnDisable()
        {
            if (blobCollider.IsCreated)
            {
                blobCollider.Dispose();
            }
        }

        public async UniTask LoadChunk(Chunk chunk)
        {
            chunkMaskHandler.RemoveMask(chunk);
            await LoadAdjacentChunks(chunk);

            waveFunction.Propagate();
            await Run(chunk);

            if (shouldCombine)
            {
                await UniTask.DelayFrame(5);
                CombineMeshes();
                
                foreach (Chunk chunkToClear in waveFunction.Chunks.Values)
                {
                    chunkToClear.ClearSpawnedMeshes(waveFunction.GameObjectPool);
                }
            }

            OnChunkGenerated?.Invoke(chunk);
        }

        private async Task Run(Chunk chunk)
        {
            Stopwatch watch = Stopwatch.StartNew();
            while (!chunk.AllCollapsed)
            {
                ChunkIndex index = waveFunction.Iterate(chunk);
                OnCellCollapsed?.Invoke(index);

                if (watch.ElapsedMilliseconds < maxMillisecondsPerFrame) continue;

                await UniTask.NextFrame();
                if (chillTimeMs > 0)
                {
                    await UniTask.Delay(chillTimeMs);
                }

                watch.Restart();
            }
        }

        private async UniTask LoadAdjacentChunks(Chunk chunk)
        {
            List<Chunk> adjacentChunks = new List<Chunk>();
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
                chunkMaskHandler.CreateMask(adjacent, Math.IntToAdjacency(new int2(-x, -z)));
                adjacentChunks.Add(adjacent);

                if (enemySpawnHandler.ShouldSetSpawnPoint(chunkIndex, out int difficulty))
                {
                    SetEnemySpawn(adjacent, DirectionUtility.Int2ToDirection(new int2(-x, -z)), difficulty);
                }

                
            }

            waveFunction.Propagate();
            for (int i = 0; i < adjacentChunks.Count; i++)
            {
                await Run(adjacentChunks[i]);
                OnLockedChunkGenerated?.Invoke(adjacentChunks[i]);
            }
        }

        private void SetEnemySpawn(Chunk chunk, Direction direction, int difficulty)
        {
            float middle = (chunk.Depth - 1) / 2.0f;

            bool isHorizontal = direction is Direction.Right or Direction.Left;
            int edge = isHorizontal 
                ? direction == Direction.Right ? chunk.Width - 1 : 0
                : direction == Direction.Forward ? chunk.Depth - 1 : 0;

            int end = isHorizontal ? chunk.Depth : chunk.Width;
            for (int i = 2; i < end - 2; i++)
            {
                int x = isHorizontal ? edge : i;
                int z = isHorizontal ? i : edge;
            
                List<PrototypeData> prots = Mathf.Abs(middle - (isHorizontal ? z : x)) <= 1
                    ? new List<PrototypeData> { defaultPrototypeInfoData.Prototypes[enemyGateIndex], defaultPrototypeInfoData.Prototypes[enemyGateIndex + 1], defaultPrototypeInfoData.Prototypes[enemyGateIndex + 2], defaultPrototypeInfoData.Prototypes[enemyGateIndex + 3] }
                    : new List<PrototypeData> { defaultPrototypeInfoData.Prototypes[fullTreeIndex] };

                chunk.Cells[x, 0, z] = new Cell(false, chunk.Cells[x, 0, z].Position, prots);
                ChunkIndex index = new ChunkIndex(chunk.ChunkIndex, new int3(x, 0, z));
                waveFunction.CellStack.Push(index);
            }

            Vector3 pos = isHorizontal
                ? (chunk.Cells[edge, 0, Mathf.FloorToInt(middle)].Position + chunk.Cells[edge, 0, Mathf.CeilToInt(middle)].Position) / 2.0f
                : (chunk.Cells[Mathf.FloorToInt(middle), 0, edge].Position + chunk.Cells[Mathf.CeilToInt(middle), 0, edge].Position) / 2.0f;
            enemySpawnHandler.SetEnemySpawn(pos - (Vector3)DirectionUtility.DirectionToInt2(direction).XyZ(), chunk.ChunkIndex, difficulty);
        }

        private void CombineMeshes()
        {
            Mesh mesh = GetComponent<MeshCombiner>().CombineMeshes();
            blobCollider = MeshCollider.Create(mesh, new CollisionFilter
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

            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
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
        }
    }


}
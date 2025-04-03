using MeshCollider = Unity.Physics.MeshCollider;
using Material = Unity.Physics.Material;
using Collider = Unity.Physics.Collider;
using Random = UnityEngine.Random;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using System.Diagnostics;
using Unity.Transforms;
using Unity.Entities;
using Unity.Physics;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;

namespace WaveFunctionCollapse
{
    public class GroundGenerator : MonoBehaviour, IChunkWaveFunction
    {
        public event Action<Chunk> OnChunkGenerated;
        public event Action<Cell> OnCellCollapsed;

        [Title("Wave Function")]
        [SerializeField]
        private ChunkWaveFunction waveFunction;

        [SerializeField]
        private Vector3Int chunkSize;
        
        [SerializeField]
        private PrototypeInfoData defaultPrototypeInfoData;
        
        [SerializeField]
        private PrototypeInfoData treePrototypeInfoData;
        
        [Title("Settings")]
        [SerializeField]
        private int chillTimeMs;

        [SerializeField]
        private float maxMillisecondsPerFrame = 4;

        [SerializeField]
        private bool shouldCombine = true;

        [Title("Debug")]
        [SerializeField]
        private bool shouldRun = true;
        
        private BlobAssetReference<Collider> blobCollider;

        public ChunkWaveFunction ChunkWaveFunction => waveFunction;
        public Vector3 ChunkScale => Vector3.one;
        
        private void Start()
        {
            if (!waveFunction.Load(this))
            {
                return;
            }
            waveFunction.ParentTransform = transform;
            
            Chunk chunk = waveFunction.LoadChunk(int3.zero, chunkSize, defaultPrototypeInfoData, false);
            
            if (shouldRun)
                LoadChunk(chunk).Forget(Debug.LogError);
        }

        private List<Chunk> LoadAdjacentChunks(Chunk chunk)
        {
            List<Chunk> chunks = new List<Chunk> { chunk };
            

            return chunks;
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
            await LoadAdjacentTrees(chunk);

            await Run(chunk);

            if (shouldCombine)
            {
                CombineMeshes();
            }

            OnChunkGenerated?.Invoke(chunk);
        }

        private async Task Run(Chunk chunk)
        {
            Stopwatch watch = Stopwatch.StartNew();
            while (!chunk.AllCollapsed)
            {
                Cell collapsedCell = waveFunction.Iterate(chunk);
                OnCellCollapsed?.Invoke(collapsedCell);

                if (watch.ElapsedMilliseconds < maxMillisecondsPerFrame) continue;

                await UniTask.NextFrame();
                if (chillTimeMs > 0)
                {
                    await UniTask.Delay(chillTimeMs);
                }

                watch.Restart();
            }
        }

        private async UniTask LoadAdjacentTrees(Chunk chunk)
        {
            for (int x = -1; x <= 1; x++)
            for (int z = -1; z <= 1; z++)
            {
                if (x == 0 && z == 0 || x != 0 && z != 0)
                {
                    continue;
                }
                
                int3 chunkIndex = new int3(chunk.ChunkIndex.x + x * chunk.width * 2, 0, chunk.ChunkIndex.z + z * chunk.depth * 2);
                if (waveFunction.Chunks.ContainsKey(chunkIndex))
                {
                    continue;
                }
                
                Chunk adjacent = waveFunction.LoadChunk(chunkIndex, chunkSize, treePrototypeInfoData, false); 
                await Run(adjacent);
            }
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
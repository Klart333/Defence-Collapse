using System.Collections.Generic;
using Random = System.Random;
using Sirenix.OdinInspector;
using WaveFunctionCollapse;
using Unity.Mathematics;
using Unity.Entities;
using Effects.ECS;
using UnityEngine;
using Gameplay;
using System;
using Enemy.ECS;
using Pathfinding;
using Unity.Collections;

namespace Enemy
{
    public class EnemySpawnHandler : MonoBehaviour
    {
        public static SpawnDataUtility SpawnDataUtility;
        
        [Title("References")]
        [SerializeField]
        private GroundGenerator groundGenerator;
        
        [SerializeField]
        private SpawnDataUtility spawnDataUtility;

        private Dictionary<ChunkIndex, Entity> spawnedEntities = new Dictionary<ChunkIndex, Entity>();
        
        private EntityManager entityManager;
        private Entity spawnPrefab;
        private Random random;
        
        private int spawnIndex;

        private void Awake()
        {
            SpawnDataUtility = spawnDataUtility;
        }

        private void OnEnable()
        {
            groundGenerator.OnChunkGenerated += OnChunkUnlocked;
            
            random = new Random(GameManager.Instance?.Seed ?? -1);
            
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            spawnPrefab = entityManager.CreateEntity(typeof(SpawnPointComponent), typeof(Prefab));
        }

        private void OnDisable()
        {
            groundGenerator.OnChunkGenerated -= OnChunkUnlocked;
        }

        private void OnChunkUnlocked(Chunk chunk)
        {
            List<ChunkIndex> toRemove = new List<ChunkIndex>();
            foreach (ChunkIndex index in spawnedEntities.Keys)
            {
                if (math.all(index.Index == chunk.ChunkIndex))
                {
                    toRemove.Add(index);
                }
            }

            NativeArray<Entity> entitiesToRemove = new NativeArray<Entity>(toRemove.Count, Allocator.Temp);
            for (int i = 0; i < toRemove.Count; i++)
            {
                entitiesToRemove[i] = spawnedEntities[toRemove[i]];
                spawnedEntities.Remove(toRemove[i]);
            }
            
            entityManager.DestroyEntity(entitiesToRemove);
            entitiesToRemove.Dispose();

            spawnIndex = 0;
            foreach (var kvp in spawnedEntities)
            {
                float3 position = ChunkWaveUtility.GetPosition(kvp.Key, groundGenerator.ChunkScale, groundGenerator.ChunkWaveFunction.CellSize);
                entityManager.SetComponentData(kvp.Value, new SpawnPointComponent
                {
                    Position = position,
                    Index = spawnIndex++,
                    Random = Unity.Mathematics.Random.CreateFromIndex((uint)random.Next(1, 20000000))
                });
            }
        }

        public void AddSpawnPoints(List<ChunkIndex> cells)
        {
            foreach (ChunkIndex chunkIndex in cells)
            {
                if (spawnedEntities.ContainsKey(chunkIndex)) continue;
                
                CreateEntity(chunkIndex);
            }
        }

        private void CreateEntity(ChunkIndex chunkIndex)
        {
            float3 position = ChunkWaveUtility.GetPosition(chunkIndex, groundGenerator.ChunkScale, groundGenerator.ChunkWaveFunction.CellSize);
            Entity entity = entityManager.Instantiate(spawnPrefab);
            entityManager.SetComponentData(entity, new SpawnPointComponent
            {
                Position = position,
                Index = spawnIndex++,
                Random = Unity.Mathematics.Random.CreateFromIndex((uint)random.Next(1, 20000000))
            });
            
            spawnedEntities.Add(chunkIndex, entity);
        }
    }
}
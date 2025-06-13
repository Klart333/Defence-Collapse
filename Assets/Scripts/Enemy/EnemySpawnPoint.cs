using Random = Unity.Mathematics.Random;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Transforms;
using Unity.Entities;
using UnityEngine;
using Effects.ECS;
using Gameplay;

namespace Enemy
{
    public class EnemySpawnPoint : PooledMonoBehaviour
    {
        [Title("Spawning")]
        [SerializeField]
        private SpawnDataUtility spawnDataUtility;
        
        [SerializeField, Tooltip("How many levels the spawnpoint needs until another entity is created")]
        private int spawnerFrequency = 10;
        
        private readonly List<Entity> entities = new List<Entity>();
        
        private EntityManager entityManager;

        private int spawnLevel;
        
        public int SpawnLevel => spawnLevel;
        public int LevelFrequency => spawnerFrequency;
        public int BaseDifficulty { get; set; }
        public EnemySpawnHandler EnemySpawnHandler { get; set; }

        private void Awake()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        private void OnEnable()
        {
            Events.OnWaveStarted += OnWaveStarted;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            
            Events.OnWaveStarted -= OnWaveStarted;
            spawnLevel = 0;

#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode &&
                UnityEditor.EditorApplication.isPlaying)
            {
                return;
            }
#endif

            if (GameManager.Instance.IsGameOver)
            {
                return;
            }
            
            foreach (Entity entity in entities)
            {
                entityManager.DestroyEntity(entity);
            }
        }

        private void OnWaveStarted()
        {
            if (spawnLevel % Mathf.Max(1, spawnerFrequency - BaseDifficulty) == 0)
            {
                CreateEntity();
            }

            spawnLevel++;
            for (int i = 0; i < entities.Count; i++)
            {
                SpawnPointComponent comp = spawnDataUtility.GetSpawnPointData(BaseDifficulty * spawnLevel - i * spawnerFrequency / 2, EnemySpawnHandler.WaveCount);
                comp.Timer = i * 5;
                entityManager.SetComponentData(entities[i], comp);
                entityManager.AddComponent<SpawningTag>(entities[i]);
            }
        }
        
        private void CreateEntity()
        {
            ComponentType[] componentTypes = 
            {
                typeof(LocalTransform),
                typeof(SpawnPointComponent),
            };
        
            Entity entity = entityManager.CreateEntity(componentTypes);
            entityManager.SetComponentData(entity, LocalTransform.FromPosition(transform.position));
                
            entities.Add(entity);
        }
    }

    public struct SpawnPointComponent : IComponentData
    {
        public float SpawnRate;
        public int EnemyIndex;
        public float Timer;
        public int Amount;
    }

    public struct SpawningTag : IComponentData { }
}
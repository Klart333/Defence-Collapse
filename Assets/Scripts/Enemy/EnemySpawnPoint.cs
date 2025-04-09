using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

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
        private int waveLevel;
        
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
            
            foreach (Entity entity in entities)
            {
                entityManager.DestroyEntity(entity);
            }
        }

        private void OnWaveStarted()
        {
            waveLevel++;

            if (spawnLevel % spawnerFrequency == 0)
            {
                CreateEntity();
            }

            spawnLevel++;
            for (int i = 0; i < entities.Count; i++)
            {
                SpawnPointComponent comp = spawnDataUtility.GetSpawnPointData(spawnLevel - i * spawnerFrequency / 2, waveLevel);
                comp.Timer = i * 5;
                entityManager.SetComponentData(entities[i], comp);
                entityManager.AddComponent<SpawningTag>(entities[i]);
            }

        }
        
        private void CreateEntity()
        {
            ComponentType[] componentTypes = new ComponentType[2]
            {
                typeof(LocalTransform),
                typeof(SpawnPointComponent),
            };
        
            var entity = entityManager.CreateEntity(componentTypes);
            entityManager.SetComponentData(entity, new LocalTransform
            {
                Position = transform.position,
            });
                
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
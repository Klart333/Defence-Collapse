using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Enemy
{
    public class Portal : PooledMonoBehaviour
    {
        [Title("Portal")]
        [SerializeField]
        private LayerMask regularMask;

        [SerializeField]
        private LayerMask hoverMask;

        [SerializeField]
        private MeshRenderer[] renderers;

        [SerializeField]
        private GameObject unlockCanvas;

        [Title("Spawning")]
        [SerializeField]
        private SpawnDataUtility spawnDataUtility;
        
        [SerializeField, Tooltip("How many levels the spawnpoint needs until another entity is created")]
        private int spawnerFrequency = 10;
        
        private readonly List<Entity> entities = new List<Entity>();
        
        private EntityManager entityManager;

        private int spawnLevel;
        private int waveLevel;
        private bool hovered;
        
        public bool Locked { get; private set; } = true;

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
        }

        private void Update()
        {
            if (unlockCanvas.activeSelf && !hovered && InputManager.Instance.GetFire && !InputManager.Instance.MouseOverUI())
            {
                unlockCanvas.SetActive(false);
            }
        }

        private void OnMouseEnter()
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].gameObject.layer = (int)Mathf.Log(hoverMask.value, 2);
            }

            hovered = true;
        }

        private void OnMouseExit()
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].gameObject.layer = (int)Mathf.Log(regularMask.value, 2);
            }

            hovered = false;
        }

        private void OnMouseDown()
        {
            if (!Locked)
            {
                return;
            }

            unlockCanvas.SetActive(true);
        }

        public void Unlock()
        {
            Locked = false;
            unlockCanvas.SetActive(false);
        }

        private void OnWaveStarted()
        {
            waveLevel++;
            if (Locked) return;

            Debug.Log("Wave Started");
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
                Debug.Log("Spawning " + comp.Amount + " Enemy Index" + comp.EnemyIndex);
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
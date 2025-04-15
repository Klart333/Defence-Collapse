using System.Collections.Generic;
using DataStructures.Queue.ECS;
using WaveFunctionCollapse;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Entities;
using UnityEngine;
using Effects.ECS;
using System;
using UnityEngine.Assertions;

namespace Buildings.District
{
    [System.Serializable]
    public abstract class DistrictState : IAttacker, IDisposable
    {
        public event Action OnAttack;
        
        public float Range { get; set; }

        protected readonly Dictionary<ChunkIndex, List<int>> cachedChunkIndexes = new Dictionary<ChunkIndex, List<int>>();
        protected readonly HashSet<int3> destroyedIndexes = new HashSet<int3>();
        
        protected DamageInstance lastDamageDone;
        protected EntityManager entityManager;
        protected DistrictData districtData;
        protected Stats stats;

        private float totalDamageDealt;
        
        public abstract Attack Attack { get; }
        public DamageInstance LastDamageDone => lastDamageDone;
        public Stats Stats => stats;
        public Vector3 OriginPosition { get; protected set; }
        public Vector3 AttackPosition { get; set; }
        public Chunk[] Chunks { get; }
        public int Key { get; set; }

        protected DistrictState(DistrictData districtData, Vector3 position, int key, Chunk[] chunks)
        {
            this.districtData = districtData;
            OriginPosition = position;
            Chunks = chunks;

            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            Key = key;
            
            CollisionSystem.DamageDoneEvent.Add(key, OnDamageDone);

            for (int i = 0; i < chunks.Length; i++)
            {
                if (chunks[i].AdjacentChunks[2] != null)
                {
                    continue; // Remove if a state uses the below chunks in future
                }
                ChunkIndex? index = BuildingManager.Instance.GetIndex(chunks[i].Position + BuildingManager.Instance.GridScale / 2.0f);
                Assert.IsTrue(index.HasValue);
                if (!cachedChunkIndexes.ContainsKey(index.Value))
                {
                    cachedChunkIndexes.Add(index.Value, new List<int>{i});
                }
                else
                {
                    cachedChunkIndexes[index.Value].Add(i);
                }
            }
            
            Events.OnBuildingDestroyed += OnBuildingDestroyed;
        }

        private void OnBuildingDestroyed(ChunkIndex chunkIndex)
        {
            if (!cachedChunkIndexes.TryGetValue(chunkIndex, out List<int> indexes)) return;
            
            for (int i = 0; i < indexes.Count; i++)
            {
                destroyedIndexes.Add(Chunks[indexes[i]].ChunkIndex);
            }
        }
        
        public abstract void OnStateEntered();
        public abstract void Update();
        public abstract void OnSelected(Vector3 pos);
        public abstract void OnDeselected();
        public abstract void Die();
        public abstract void OnWaveStart();
        
        private void OnDamageDone(Entity entity)
        {
            DamageComponent damage = entityManager.GetComponentData<DamageComponent>(entity);
            PositionComponent transform = entityManager.GetComponentData<PositionComponent>(entity);
            
            totalDamageDealt += damage.Damage;
            lastDamageDone = new DamageInstance
            {
                Damage = damage.Damage,
                AttackPosition = transform.Position,
                Source = this,
            };

            if (damage.TriggerDamageDone)
            {
                Attack?.OnDoneDamage(this);
            }
        }

        public void OnUnitDoneDamage(DamageInstance damageInstance)
        {
            lastDamageDone = damageInstance;

            Attack?.OnDoneDamage(this);
        }

        public virtual void OnUnitKill()
        {

        }

        public void Dispose()
        {
            Events.OnBuildingDestroyed -= OnBuildingDestroyed;
        }
    }

    #region Archer

    public class ArcherState : DistrictState
    {
        private readonly List<Entity> spawnedEntities = new List<Entity>();
        private readonly Dictionary<Entity, int3> entityIndexes = new Dictionary<Entity, int3>();
        
        private readonly Attack attack;
        private readonly TowerData archerData;

        private GameObject rangeIndicator;
        private bool selected;
        
        public override Attack Attack => attack;

        public ArcherState(DistrictData districtData, TowerData archerData, Chunk[] chunks, Vector3 position, int key) : base(districtData, position, key, chunks)
        {
            this.archerData = archerData;
            Range = archerData.Range;

            attack = new Attack(archerData.BaseAttack);
            stats = new Stats(archerData.Stats);

            SpawnEntities(chunks);
        }

        private void SpawnEntities(Chunk[] chunks)
        {
            List<Chunk> topChunks = DistrictUtility.GetTopChunks(chunks);
            ComponentType[] componentTypes =
            {
                typeof(LocalTransform),
                typeof(RangeComponent),
                typeof(EnemyTargetComponent),
                typeof(AttackSpeedComponent),
            };
            
            int count = topChunks.Count;
            float delay = (1.0f / archerData.Stats.AttackSpeed.Value) / count;
            for (int i = 0; i < count; i++)
            {
                Entity spawnedEntity = entityManager.CreateEntity(componentTypes);
                entityManager.SetComponentData(spawnedEntity, new LocalTransform { Position = topChunks[i].Position });
                entityManager.SetComponentData(spawnedEntity, new RangeComponent { Range = Range }); // These could be made to blob assets, probably easier to change then
                entityManager.SetComponentData(spawnedEntity, new AttackSpeedComponent
                {
                    AttackSpeed = 1.0f / archerData.Stats.AttackSpeed.Value, 
                    Timer = delay * i
                });
                spawnedEntities.Add(spawnedEntity);
                
                entityIndexes.Add(spawnedEntity, topChunks[i].ChunkIndex);
            }
        }

        public override void OnStateEntered()
        {

        }

        public override void OnSelected(Vector3 pos)
        {
            if (selected) return;
            
            selected = true;
            rangeIndicator = archerData.RangeIndicator.GetAtPosAndRot<PooledMonoBehaviour>(pos, Quaternion.identity).gameObject;
            rangeIndicator.transform.localScale = new Vector3(Range * 2.0f, 0.01f, Range * 2.0f);
        }

        public override void OnDeselected()
        {
            selected = false;
            if (rangeIndicator != null)
            {
                rangeIndicator.SetActive(false);
                rangeIndicator = null;
            }
        }

        public override void Update()
        {
            for (int i = 0; i < spawnedEntities.Count; i++)
            {
                if (destroyedIndexes.Contains(entityIndexes[spawnedEntities[i]]))
                {
                    Debug.Log($"{entityIndexes[spawnedEntities[i]]} is destroyed");
                    continue;
                }
                
                EnemyTargetAspect aspect = entityManager.GetAspect<EnemyTargetAspect>(spawnedEntities[i]);
                if (aspect.CanAttack() && entityManager.Exists(aspect.EnemyTargetComponent.ValueRO.Target))
                {
                    aspect.RestTimer();
                    OriginPosition = aspect.LocalTransform.ValueRO.Position;
                    PerformAttack(entityManager.GetComponentData<LocalTransform>(aspect.EnemyTargetComponent.ValueRO.Target).Position);
                    break;
                }
            }
        }

        private void PerformAttack(float3 targetPosition)
        {
            AttackPosition = targetPosition;
            attack.TriggerAttack(this);
        }

        public override void OnWaveStart()
        {
            
        }

        public override void Die()
        {

        }
    }

    #endregion

    #region Bomb

    public class BombState : DistrictState
    {
        private readonly List<Entity> spawnedEntities = new List<Entity>();
        private readonly Dictionary<Entity, int3> entityIndexes = new Dictionary<Entity, int3>();

        private readonly TowerData bombData;
        private readonly Attack attack;

        private GameObject rangeIndicator;
        private bool selected;
        
        public override Attack Attack => attack;
        
        public BombState(DistrictData districtData, TowerData bombData, Chunk[] chunks, Vector3 position, int key) : base(districtData, position, key, chunks)
        {
            this.bombData = bombData;
            Range = bombData.Range;

            stats = new Stats(bombData.Stats);

            SpawnEntities(chunks);
        }

        private void SpawnEntities(IEnumerable<Chunk> chunks)
        {
            List<Chunk> topChunks = DistrictUtility.GetTopChunks(chunks); // Maybe edges shouldn't shoot?
            ComponentType[] componentTypes =
            {
                typeof(LocalTransform),
                typeof(RangeComponent),
                typeof(EnemyTargetComponent),
                typeof(AttackSpeedComponent),
            };
            
            int count = topChunks.Count;
            float delay = (1.0f / bombData.Stats.AttackSpeed.Value) / count;
            for (int i = 0; i < count; i++)
            {
                Entity spawnedEntity = entityManager.CreateEntity(componentTypes);
                entityManager.SetComponentData(spawnedEntity, new LocalTransform { Position = topChunks[i].Position });
                // These could be made to blob assets, probably easier to change then
                entityManager.SetComponentData(spawnedEntity, new RangeComponent { Range = Range });
                entityManager.SetComponentData(spawnedEntity, new AttackSpeedComponent
                {
                    AttackSpeed = 1.0f / bombData.Stats.AttackSpeed.Value, 
                    Timer = delay * i
                });
                spawnedEntities.Add(spawnedEntity);
                entityIndexes.Add(spawnedEntity, topChunks[i].ChunkIndex);
            }
        }
        
        public override void OnStateEntered()
        {

        }

        public override void OnSelected(Vector3 pos)
        {
            if (selected) return;
            
            selected = true;
            rangeIndicator = bombData.RangeIndicator.GetAtPosAndRot<PooledMonoBehaviour>(pos, Quaternion.identity).gameObject;
            rangeIndicator.transform.localScale = new Vector3(Range * 2.0f, 0.01f, Range * 2.0f);
        }

        public override void OnDeselected()
        {
            selected = false;
            
            if (rangeIndicator != null)
            {
                rangeIndicator.SetActive(false);
            }
        }

        public override void Update()
        {
            for (int i = 0; i < spawnedEntities.Count; i++)
            {
                if (destroyedIndexes.Contains(entityIndexes[spawnedEntities[i]]))
                {
                    continue;
                }
                
                EnemyTargetAspect aspect = entityManager.GetAspect<EnemyTargetAspect>(spawnedEntities[i]);
                if (aspect.CanAttack() && entityManager.Exists(aspect.EnemyTargetComponent.ValueRO.Target))
                {
                    PerformAttack(entityManager.GetComponentData<LocalTransform>(aspect.EnemyTargetComponent.ValueRO.Target).Position);
                    
                    aspect.AttackSpeedComponent.ValueRW.Timer = 0;
                }
            }
        }

        private void PerformAttack(float3 targetPosition)
        {
            AttackPosition = targetPosition;
            attack.TriggerAttack(this);
        }

        public override void OnWaveStart()
        {
            
        }

        public override void Die()
        {

        }
    }

    #endregion

    #region Mine

    public class MineState : DistrictState
    {
        private struct MineInstance
        {
            public float Timer;
            public readonly Vector3 Position;
            public readonly int3 ChunkIndex;

            public MineInstance(Vector3 position, float timer, int3 chunkIndex)
            {
                Position = position;
                Timer = timer;
                ChunkIndex = chunkIndex;
            }
        }
        
        private readonly List<MineInstance> mineChunks = new List<MineInstance>();
        
        private readonly TowerData mineData;

        public override Attack Attack { get; }
        
        public MineState(DistrictData districtData, TowerData mineData, Chunk[] chunks, Vector3 position, int key) : base(districtData, position, key, chunks)
        {
            this.mineData = mineData;
            
            stats = new Stats(mineData.Stats);
            Attack = new Attack(mineData.BaseAttack);

            for (int i = 0; i < chunks.Length; i++)
            {
                if (chunks[i].AdjacentChunks[2] != null)
                {
                    continue;
                }
                
                mineChunks.Add(new MineInstance(chunks[i].Position, 0, chunks[i].ChunkIndex));
            }
        }
        
        public override void OnStateEntered()
        {

        }

        public override void OnSelected(Vector3 pos)
        {
            
        }

        public override void OnDeselected()
        {
           
        }

        public override void Update()
        {
            float mineSpeed = 1.0f / stats.AttackSpeed.Value;
            for (int i = 0; i < mineChunks.Count; i++)
            {
                MineInstance instance = mineChunks[i];
                if (destroyedIndexes.Contains(instance.ChunkIndex))
                {
                    continue;
                }
                
                instance.Timer += Time.deltaTime * districtData.GameSpeed.Value;
                if (mineChunks[i].Timer >= mineSpeed)
                {
                    OriginPosition = mineChunks[i].Position;
                    PerformAttack();
                    
                    instance.Timer = 0;
                }

                mineChunks[i] = instance;
            }
        }

        private void PerformAttack()
        {
            Attack.TriggerAttack(this);
        }

        public override void OnWaveStart()
        {
            
        }

        public override void Die()
        {

        }
    }

    #endregion

}

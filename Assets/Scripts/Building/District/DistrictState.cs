using System.Collections.Generic;
using DataStructures.Queue.ECS;
using WaveFunctionCollapse;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Entities;
using UnityEngine;
using Effects.ECS;
using System;
using System.Linq;

namespace Buildings.District
{
    [System.Serializable]
    public abstract class DistrictState : IAttacker
    {
        public event Action OnAttack;
        
        public float Range { get; set; }
        
        protected DamageInstance lastDamageDone;
        protected EntityManager entityManager;
        protected Stats stats;

        private float totalDamageDealt;
        
        public DamageInstance LastDamageDone => lastDamageDone;
        public Stats Stats => stats;
        
        public Vector3 OriginPosition { get; protected set; }
        public Vector3 AttackPosition { get; set; }
        public DistrictData DistrictData { get; }
        public abstract Attack Attack { get; }
        public int Key { get; set; }

        protected DistrictState(DistrictData districtData, Vector3 position, int key)
        {
            DistrictData = districtData;
            OriginPosition = position;

            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            Key = key;
            
            CollisionSystem.DamageDoneEvent.Add(key, OnDamageDone);
        }
        
        public abstract void Update();
        public abstract void OnSelected(Vector3 pos);
        public abstract void OnDeselected();
        public abstract void OnIndexesDestroyed(HashSet<int3> destroyedIndexes);
        public abstract void Die();

        public virtual void OnWaveStart()
        {
            
        }
        
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
    }

    #region Entity District State

    public abstract class EntityDistrictState : DistrictState
    {
        protected readonly HashSet<Entity> spawnedEntities = new HashSet<Entity>();
        protected readonly Dictionary<int3, Entity> entityIndexes = new Dictionary<int3, Entity>();
        
        protected EntityDistrictState(DistrictData districtData, Vector3 position, int key) : base(districtData, position, key)
        {
        }
        
        protected abstract List<Chunk> GetEntityChunks();
        
        protected void SpawnEntities()
        {
            List<Chunk> topChunks = GetEntityChunks();
            ComponentType[] componentTypes =
            {
                typeof(LocalTransform),
                typeof(RangeComponent),
                typeof(EnemyTargetComponent),
                typeof(AttackSpeedComponent),
            };
            
            int count = topChunks.Count;
            float delay = (1.0f / stats.AttackSpeed.Value) / count;
            for (int i = 0; i < count; i++)
            {
                Entity spawnedEntity = entityManager.CreateEntity(componentTypes);
                entityManager.SetComponentData(spawnedEntity, new LocalTransform { Position = topChunks[i].Position });
                // These could be made to blob assets, probably easier to change then
                entityManager.SetComponentData(spawnedEntity, new RangeComponent { Range = Range });
                entityManager.SetComponentData(spawnedEntity, new AttackSpeedComponent
                {
                    AttackSpeed = 1.0f / stats.AttackSpeed.Value, 
                    Timer = delay * i
                });
                spawnedEntities.Add(spawnedEntity);
                entityIndexes.Add(topChunks[i].ChunkIndex, spawnedEntity);
            }
        }
        
        protected void UpdateEntities()
        {
            foreach (Entity entity in spawnedEntities)
            {
                EnemyTargetAspect aspect = entityManager.GetAspect<EnemyTargetAspect>(entity);
                if (aspect.CanAttack() && entityManager.Exists(aspect.EnemyTargetComponent.ValueRO.Target))
                {
                    aspect.RestTimer();
                    OriginPosition = aspect.LocalTransform.ValueRO.Position;
                    PerformAttack(entityManager.GetComponentData<LocalTransform>(aspect.EnemyTargetComponent.ValueRO.Target).Position);
                    break;
                }
            }
        }
        
        protected void PerformAttack(float3 targetPosition)
        {
            AttackPosition = targetPosition;
            Attack.TriggerAttack(this);
        }

        public override void OnIndexesDestroyed(HashSet<int3> destroyedIndexes)
        {
            NativeArray<Entity> entitiesToDestroy = new NativeArray<Entity>(destroyedIndexes.Count, Allocator.Temp);
            int index = 0;
            foreach (int3 destroyedIndex in destroyedIndexes)
            {
                if (!entityIndexes.TryGetValue(destroyedIndex, out Entity entity)) continue;
                
                entitiesToDestroy[index++] = entity;
                spawnedEntities.Remove(entity);
            }
            
            entityManager.DestroyEntity(entitiesToDestroy);
            
            entitiesToDestroy.Dispose();
        }
    }

    #endregion

    #region Archer

    public class ArcherState : EntityDistrictState
    {
        private readonly TowerData archerData;

        private GameObject rangeIndicator;
        private bool selected;
        
        public override Attack Attack { get; }

        public ArcherState(DistrictData districtData, TowerData archerData, Vector3 position, int key) : base(districtData, position, key)
        {
            this.archerData = archerData;
            Range = archerData.Range;

            stats = new Stats(archerData.Stats);
            Attack = new Attack(archerData.BaseAttack);
            SpawnEntities();
        }

        protected override List<Chunk> GetEntityChunks()
        {
            return DistrictData.DistrictChunks.Values.ToList();
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
            UpdateEntities();
        }

        public override void Die()
        {

        }
    }

    #endregion

    #region Bomb

    public class BombState : EntityDistrictState
    {
        private readonly TowerData bombData;

        private GameObject rangeIndicator;
        private bool selected;
        
        public override Attack Attack { get; }
        
        public BombState(DistrictData districtData, TowerData bombData, Vector3 position, int key) : base(districtData, position, key)
        {
            this.bombData = bombData;
            Range = bombData.Range;

            stats = new Stats(bombData.Stats);
            Attack = new Attack(bombData.BaseAttack);

            SpawnEntities();
        }

        protected override List<Chunk> GetEntityChunks()
        {
            return DistrictData.DistrictChunks.Values.ToList();
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
                rangeIndicator = null;
            }
        }

        public override void Update()
        {
            UpdateEntities();
        }
        
        public override void Die()
        {

        }
    }
    
    #endregion

    #region Town Hall

    public class TownHallState : EntityDistrictState
    {
        private TowerData townHallData;
        
        public override Attack Attack { get; }
        
        public TownHallState(DistrictData districtData, TowerData townHallData, Vector3 position, int key) : base(districtData, position, key)
        {
            this.townHallData = townHallData;
            Range = townHallData.Range;

            stats = new Stats(townHallData.Stats);
            Attack = new Attack(townHallData.BaseAttack);
            SpawnEntities();
            
            Events.OnWaveEnded += OnWaveEnded;
        }

        private void OnWaveEnded()
        {
            Debug.Log("10 points to grifflindor");
        }

        protected override List<Chunk> GetEntityChunks()
        {
            return new List<Chunk> { DistrictData.DistrictChunks.Values.First() };
        }

        public override void Update()
        {
            UpdateEntities();
        }

        public override void OnSelected(Vector3 pos)
        {
            
        }

        public override void OnDeselected()
        {
            
        }

        public override void Die()
        {
            Events.OnWaveEnded -= OnWaveEnded;
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
        
        public bool IsCapitol { get; set; }
        public override Attack Attack { get; }
        
        public MineState(DistrictData districtData, TowerData mineData, Vector3 position, int key) : base(districtData, position, key)
        {
            this.mineData = mineData;
            
            stats = new Stats(mineData.Stats);
            Attack = new Attack(mineData.BaseAttack);

            foreach (Chunk chunk in districtData.DistrictChunks.Values)
            {
                if (chunk.AdjacentChunks[2] != null)
                {
                    continue;
                }
                
                mineChunks.Add(new MineInstance(chunk.Position, 0, chunk.ChunkIndex));
            }
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
                
                instance.Timer += Time.deltaTime * DistrictData.GameSpeed.Value;
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

        public override void OnIndexesDestroyed(HashSet<int3> destroyedIndexes)
        {
            foreach (int3 destroyedIndex in destroyedIndexes)
            {
                for (int i = 0; i < mineChunks.Count; i++)
                {
                    if (!math.all(mineChunks[i].ChunkIndex == destroyedIndex)) continue;
                    
                    mineChunks.RemoveAtSwapBack(i);
                    break;
                }
            }
        }

        public override void Die()
        {

        }
    }

    #endregion

}

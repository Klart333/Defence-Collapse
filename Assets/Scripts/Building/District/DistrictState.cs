using System.Collections.Generic;
using DataStructures.Queue.ECS;
using WaveFunctionCollapse;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Entities;
using UnityEngine;
using Effects.ECS;
using System.Linq;
using System;
using Gameplay.Research;

namespace Buildings.District
{
    [System.Serializable]
    public abstract class DistrictState : IAttacker
    {
        public event Action OnAttack;
        
        protected DamageInstance lastDamageDone;
        protected EntityManager entityManager;
        protected Stats stats;

        private float totalDamageDealt;
        
        public DamageInstance LastDamageDone => lastDamageDone;
        public Stats Stats => stats;
        
        public abstract List<IUpgradeStat> UpgradeStats { get; }
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
        protected readonly Dictionary<int2, Entity> entityIndexes = new Dictionary<int2, Entity>();
        private readonly World defaultWorld;
        
        protected EntityDistrictState(DistrictData districtData, Vector3 position, int key) : base(districtData, position, key)
        {
            defaultWorld = World.DefaultGameObjectInjectionWorld;
        }
        
        protected abstract List<Chunk> GetEntityChunks();
        
        public void SpawnEntities()
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
            float attackSpeedValue = 1.0f / stats.AttackSpeed.Value;
            float delay = attackSpeedValue / count;
            
            NativeArray<Entity> entities = new NativeArray<Entity>(count, Allocator.Temp);
            Entity srcEntity = entityManager.CreateEntity(componentTypes);
            entityManager.SetComponentData(srcEntity, new RangeComponent { Range = stats.Range.Value });
            
            entityManager.Instantiate(srcEntity, entities);
            for (int i = 0; i < count; i++)
            {
                Entity spawnedEntity = entities[i];
                entityManager.SetComponentData(spawnedEntity, new LocalTransform { Position = topChunks[i].Position });
                entityManager.SetComponentData(spawnedEntity, new AttackSpeedComponent
                {
                    AttackSpeed = attackSpeedValue, 
                    Timer = delay * i
                });
                spawnedEntities.Add(spawnedEntity);
                entityIndexes.Add(topChunks[i].ChunkIndex.xz, spawnedEntity);
            }

            entities.Dispose();
            stats.Range.OnValueChanged += RangeChanged;
            stats.AttackSpeed.OnValueChanged += AttackSpeedChanged;
        }

        private void AttackSpeedChanged()
        {
            float attackSpeedValue = 1.0f / stats.AttackSpeed.Value;
            float delay = attackSpeedValue / entityIndexes.Count;
            int index = 0;
            foreach (Entity entity in spawnedEntities)
            {
                entityManager.SetComponentData(entity, new AttackSpeedComponent
                {
                    AttackSpeed = attackSpeedValue, 
                    Timer = index++ * delay,
                });
            }
        }

        private void RangeChanged()
        {
            foreach (Entity entity in spawnedEntities)
            {
                entityManager.SetComponentData(entity, new RangeComponent { Range = stats.Range.Value });
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
        
        public void RemoveEntities()
        {
            NativeArray<Entity> entitiesToDestroy = new NativeArray<Entity>(spawnedEntities.Count, Allocator.Temp);
            int index = 0;
            foreach (Entity entity in spawnedEntities)
            {
                entitiesToDestroy[index++] = entity;
            }
            
            entityManager.DestroyEntity(entitiesToDestroy);
            
            entityIndexes.Clear();
            spawnedEntities.Clear();
            entitiesToDestroy.Dispose();
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
                if (!entityIndexes.TryGetValue(destroyedIndex.xz, out Entity entity)) continue;
                
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
        public override List<IUpgradeStat> UpgradeStats { get; } = new List<IUpgradeStat>();
        
        private readonly TowerData archerData;

        private GameObject rangeIndicator;
        private bool selected;
        
        public override Attack Attack { get; }

        public ArcherState(DistrictData districtData, TowerData archerData, Vector3 position, int key) : base(districtData, position, key)
        {
            this.archerData = archerData;

            CreateStats();
            SpawnEntities();
            
            Attack = new Attack(archerData.BaseAttack);
            stats.Range.OnValueChanged += RangeChanged;
        }

        private void RangeChanged()
        {
            if (selected && rangeIndicator != null && rangeIndicator.activeSelf)
            {
                rangeIndicator.transform.localScale = new Vector3(stats.Range.Value * 2.0f, 0.01f, stats.Range.Value * 2.0f);
            }
        }

        private void CreateStats()
        {
            stats = new Stats(archerData.Stats);
            UpgradeStat attackSpeed = new UpgradeStat(stats.AttackSpeed as Stat, archerData.LevelDatas[0],
                "Attack Speed",
                new string[] { "Increase Attack Speed by {0}/s", "Current Attack Speed: {0}/s" },
                archerData.UpgradeIcons[0]);
            UpgradeStat damage = new UpgradeStat(stats.DamageMultiplier as Stat, archerData.LevelDatas[1],
                "Damge Multiplier",
                new string[] { "Increase Damage Multiplier by {0}x", "Current Damage Multiplier: {0}x" },
                archerData.UpgradeIcons[1]);
            UpgradeStat range = new UpgradeStat(stats.Range as Stat, archerData.LevelDatas[2],
                "Range",
                new string[] { "Increase Range by {0}", "Current Range: {0}" },
                archerData.UpgradeIcons[2]);

            UpgradeStats.Add(attackSpeed);
            UpgradeStats.Add(damage);
            UpgradeStats.Add(range);
        }

        protected override List<Chunk> GetEntityChunks()
        {
            return DistrictData.DistrictChunks.Values.ToList();
        }

        public override void OnSelected(Vector3 pos)
        {
            if (selected) return;
            
            selected = true;
            rangeIndicator = archerData.RangeIndicator.GetDisabled<PooledMonoBehaviour>().gameObject;
            rangeIndicator.transform.position = pos;
            rangeIndicator.transform.localScale = new Vector3(stats.Range.Value * 2.0f, 0.01f, stats.Range.Value * 2.0f);
            rangeIndicator.gameObject.SetActive(true);
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
        
        public override List<IUpgradeStat> UpgradeStats { get; } = new List<IUpgradeStat>();
        public override Attack Attack { get; }
        
        public BombState(DistrictData districtData, TowerData bombData, Vector3 position, int key) : base(districtData, position, key)
        {
            this.bombData = bombData;

            CreateStats();
            SpawnEntities();

            Attack = new Attack(bombData.BaseAttack);
            stats.Range.OnValueChanged += RangeChanged;
        }

        private void RangeChanged()
        {
            if (selected && rangeIndicator != null && rangeIndicator.activeSelf)
            {
                rangeIndicator.transform.localScale = new Vector3(stats.Range.Value * 2.0f, 0.01f, stats.Range.Value * 2.0f);
            }
        }

        private void CreateStats()
        {
            stats = new Stats(bombData.Stats);
            UpgradeStat attackSpeed = new UpgradeStat(stats.AttackSpeed as Stat, bombData.LevelDatas[0],
                "Attack Speed",
                new string[] { "Increase Attack Speed by {0}/s", "Current Attack Speed: {0}/s" },
                bombData.UpgradeIcons[0]);
            UpgradeStat damage = new UpgradeStat(stats.DamageMultiplier as Stat, bombData.LevelDatas[1],
                "Damge Multiplier",
                new string[] { "Increase Damage Multiplier by {0}x", "Current Damage Multiplier: {0}x" },
                bombData.UpgradeIcons[1]);
            UpgradeStat range = new UpgradeStat(stats.Range as Stat, bombData.LevelDatas[2],
                "Range",
                new string[] { "Increase Range by {0}", "Current Range: {0}" },
                bombData.UpgradeIcons[2]);

            UpgradeStats.Add(attackSpeed);
            UpgradeStats.Add(damage);
            UpgradeStats.Add(range);
        }

        protected override List<Chunk> GetEntityChunks()
        {
            return DistrictData.DistrictChunks.Values.ToList();
        }

        public override void OnSelected(Vector3 pos)
        {
            if (selected) return;
            
            selected = true;
            rangeIndicator = bombData.RangeIndicator.GetDisabled<PooledMonoBehaviour>().gameObject;
            rangeIndicator.transform.position = pos;
            rangeIndicator.transform.localScale = new Vector3(stats.Range.Value * 2.0f, 0.01f, stats.Range.Value * 2.0f);
            rangeIndicator.gameObject.SetActive(true);
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
        private readonly TowerData townHallData;
        private GameObject rangeIndicator;
        private bool selected;
        
        public override List<IUpgradeStat> UpgradeStats { get; } = new List<IUpgradeStat>();
        public override Attack Attack { get; }
        
        public TownHallState(DistrictData districtData, TowerData townHallData, Vector3 position, int key) : base(districtData, position, key)
        {
            this.townHallData = townHallData;

            CreateStats();
            SpawnEntities();
            
            Attack = new Attack(townHallData.BaseAttack);
            Events.OnWaveEnded += OnWaveEnded;
            stats.Range.OnValueChanged += RangeChanged;
        }

        private void RangeChanged()
        {
            if (selected && rangeIndicator != null && rangeIndicator.activeSelf)
            {
                rangeIndicator.transform.localScale = new Vector3(stats.Range.Value * 2.0f, 0.01f, stats.Range.Value * 2.0f);
            }
        }

        private void CreateStats()
        {
            stats = new Stats(townHallData.Stats);
            TownHallUpgradeStat townHall = new TownHallUpgradeStat(new Stat(1), townHallData.LevelDatas[0],
                "Level",
                new string[] { "Increase Level by {0}", "Current Level: {0}" },
                townHallData.UpgradeIcons[0]);

            UpgradeStats.Add(townHall);
        }
        
        private void OnWaveEnded()
        {
            ResearchManager.Instance.AddResearchPoints(10 * UpgradeStats[0].Level);
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
            if (selected) return;
            
            selected = true;
            rangeIndicator = townHallData.RangeIndicator.GetDisabled<PooledMonoBehaviour>().gameObject;
            rangeIndicator.transform.position = pos;
            rangeIndicator.transform.localScale = new Vector3(stats.Range.Value * 2.0f, 0.01f, stats.Range.Value * 2.0f);
            rangeIndicator.gameObject.SetActive(true);
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
        
        public override List<IUpgradeStat> UpgradeStats { get; } = new List<IUpgradeStat>();
        public override Attack Attack { get; }
        
        public MineState(DistrictData districtData, TowerData mineData, Vector3 position, int key) : base(districtData, position, key)
        {
            this.mineData = mineData;

            CreateStats();
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
        
        private void CreateStats()
        {
            stats = new Stats(mineData.Stats);
            UpgradeStat attackSpeed = new UpgradeStat(stats.AttackSpeed as Stat, mineData.LevelDatas[0], 
                "Mine Speed",
                new string[] { "Increase Mining speed by {0}/s", "Current Mining speed: {0}/s" },
                mineData.UpgradeIcons[0]);
            UpgradeStat damage = new UpgradeStat(stats.DamageMultiplier as Stat, mineData.LevelDatas[1], 
                "Value Multiplier",
                new string[] { "Increase Value Multiplier by {0}x", "Current Value Multiplier: {0}x" },
                mineData.UpgradeIcons[1]);

            UpgradeStats.Add(attackSpeed);
            UpgradeStats.Add(damage);
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

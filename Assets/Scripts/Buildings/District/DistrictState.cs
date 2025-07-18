﻿using Vector2 = UnityEngine.Vector2;
using System.Collections.Generic;
using WaveFunctionCollapse;
using Unity.Collections;
using Unity.Mathematics;
using Gameplay.Upgrades;
using Unity.Transforms;
using Unity.Entities;
using System.Linq;
using Effects.ECS;
using UnityEngine;
using Enemy.ECS;
using Effects;
using System;
using Juice;

namespace Buildings.District
{
    public abstract class DistrictState : IAttacker, IAttackerStatistics, IDisposable
    {
        public event Action OnStatisticsChanged;
        public event Action OnAttack;

        private readonly Dictionary<int2, List<DistrictTargetMesh>> targetMeshes = new Dictionary<int2, List<DistrictTargetMesh>>();
        private readonly Dictionary<int2, List<Entity>> entityIndexes = new Dictionary<int2, List<Entity>>();
        protected readonly HashSet<Entity> spawnedEntities = new HashSet<Entity>();
        
        protected DamageInstance lastDamageDone;
        protected EntityManager entityManager;
        protected Stats stats;


        public DamageInstance LastDamageDone => lastDamageDone;
        public Stats Stats => stats;

        public abstract List<IUpgradeStat> UpgradeStats { get; }
        public abstract CategoryType CategoryType { get; }
        protected abstract bool UseTargetMeshes { get; }
        protected abstract float AttackAngle { get; }
        protected DistrictData DistrictData { get; }
        public Vector3 OriginPosition { get; set; }
        public Vector3 AttackPosition { get; set; }
        public abstract Attack Attack { get; }
        public float DamageDone { get; set; }
        public float GoldGained { get; set; }
        public int Level { get; set; } = 1;
        public int Key { get; set; }

        protected DistrictState(DistrictData districtData, Vector3 position, int key)
        {
            DistrictData = districtData;
            OriginPosition = position;

            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            Key = key;
            
            DamageCallbackHandler.DamageDoneEvent.Add(key, OnDamageDone);
        }
        
        public abstract void Update();
        protected abstract DistrictTargetMesh GetTargetMesh(Vector3 position, Vector2 offset);
        public abstract void OnSelected(Vector3 pos);
        public abstract void OnDeselected();

        public virtual void OnIndexesDestroyed(HashSet<int3> destroyedIndexes)
        {
            NativeList<Entity> entitiesToDestroy = new NativeList<Entity>(destroyedIndexes.Count * 2, Allocator.Temp);
            foreach (int3 destroyedIndex in destroyedIndexes)
            {
                if (!entityIndexes.TryGetValue(destroyedIndex.xz, out List<Entity> entities)) continue;

                for (int i = 0; i < entities.Count; i++)
                {
                    entitiesToDestroy.Add(entities[i]);
                    spawnedEntities.Remove(entities[i]);
                }
                
                entityIndexes.Remove(destroyedIndex.xz);

                if (!targetMeshes.TryGetValue(destroyedIndex.xz, out List<DistrictTargetMesh> meshes)) continue;
                
                for (int i = 0; i < meshes.Count; i++)
                {
                    meshes[i].gameObject.SetActive(false);
                }
                targetMeshes.Remove(destroyedIndex.xz);
            }
            
            entityManager.DestroyEntity(entitiesToDestroy.AsArray());
            
            entitiesToDestroy.Dispose();
        }

        public virtual void OnWaveStart()
        {
            
        }
        
        public virtual void OnWaveEnd()
        {
            
        }
        
        private void OnDamageDone(DamageCallbackComponent damageCallback)
        {
            DamageDone += damageCallback.DamageTaken;
            OnStatisticsChanged?.Invoke();
            if (!damageCallback.TriggerDamageDone) return;
            
            lastDamageDone = new DamageInstance
            {
                Damage = damageCallback.DamageTaken,
                AttackPosition = damageCallback.Position,
                Source = this,
            };
            
            Attack?.OnDoneDamage(this);
        }

        public void OnUnitDoneDamage(DamageInstance damageInstance)
        {
            lastDamageDone = damageInstance;

            Attack?.OnDoneDamage(this);
        }

        public virtual void OnUnitKill()
        {

        }

        protected void InvokeStatisticsChanged()
        {
            OnStatisticsChanged?.Invoke();
        }
        
        public virtual void Dispose()
        {
            DamageCallbackHandler.DamageDoneEvent.Remove(Key);
                
            foreach (List<DistrictTargetMesh> meshes in targetMeshes.Values)
            {
                foreach (DistrictTargetMesh targetMesh in meshes) 
                    targetMesh.gameObject.SetActive(false);
            }
            targetMeshes.Clear();
        }

        #region Entities

        protected virtual List<QueryChunk> GetEntityChunks(out List<Vector2> offsets, out List<Vector2> directions)
        {
            offsets = new Vector2[DistrictData.DistrictChunks.Values.Count].ToList();
            directions = new Vector2[DistrictData.DistrictChunks.Values.Count].ToList();
            return DistrictData.DistrictChunks.Values.ToList();
        }
        
        public virtual void SpawnEntities()
        {
            List<QueryChunk> topChunks = GetEntityChunks(out List<Vector2> offsets, out List<Vector2> directions);
            ComponentType[] componentTypes =
            {
                typeof(DirectionRangeComponent),  
                typeof(EnemyTargetComponent),
                typeof(AttackSpeedComponent),
                typeof(LocalTransform),
                typeof(RangeComponent),
            };
            
            int count = topChunks.Count;
            float attackSpeedValue = 1.0f / stats.AttackSpeed.Value;
            float delay = attackSpeedValue / count;
            
            Entity srcEntity = entityManager.CreateEntity(componentTypes);
            entityManager.SetComponentData(srcEntity, new RangeComponent { Range = stats.Range.Value });
            
            NativeArray<Entity> entities = entityManager.Instantiate(srcEntity, count, Allocator.Temp);
            
            for (int i = 0; i < count; i++)
            {
                Entity spawnedEntity = entities[i];
                Vector3 offset = DistrictData.DistrictGenerator.ChunkScale.MultiplyByAxis(new Vector3(0.5f + offsets[i].x, 0.5f, 0.5f + offsets[i].y));
                Vector3 pos = topChunks[i].Position + offset;
                entityManager.SetComponentData(spawnedEntity, new LocalTransform { Position = pos });
                entityManager.SetComponentData(spawnedEntity, new AttackSpeedComponent
                {
                    AttackSpeed = attackSpeedValue, 
                    Timer = delay * i
                });
                entityManager.SetComponentData(spawnedEntity, new DirectionRangeComponent
                {
                    Direction = directions[i],
                    Angle = AttackAngle,
                });
                spawnedEntities.Add(spawnedEntity);

                int2 key = topChunks[i].ChunkIndex.xz;
                AddEntityToDicts(pos, directions[i], key, spawnedEntity);
            }
            
            entities.Dispose();
            stats.Range.OnValueChanged += RangeChanged;
            stats.AttackSpeed.OnValueChanged += AttackSpeedChanged;

            void AddEntityToDicts(Vector3 pos, Vector2 direction, int2 key, Entity spawnedEntity)
            {
                if (entityIndexes.TryGetValue(key, out List<Entity> list))
                {
                    list.Add(spawnedEntity);
                    if (UseTargetMeshes)
                    {
                        targetMeshes[key].Add(GetTargetMesh(pos, direction));
                    }
                }
                else
                {
                    entityIndexes.Add(key, new List<Entity> { spawnedEntity });
                    if (UseTargetMeshes)
                    {
                        targetMeshes.Add(key, new List<DistrictTargetMesh> { GetTargetMesh(pos, direction) });
                    }
                }
            }
        }

        protected virtual void AttackSpeedChanged()
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

        protected virtual void RangeChanged()
        {
            foreach (Entity entity in spawnedEntities)
            {
                entityManager.SetComponentData(entity, new RangeComponent { Range = stats.Range.Value });
            }
        }

        protected virtual void UpdateEntities()
        {
            foreach (KeyValuePair<int2, List<Entity>> keyValuePair in entityIndexes)
            {
                for (int i = 0; i < keyValuePair.Value.Count; i++)
                {
                    Entity entity = keyValuePair.Value[i];
                    EnemyTargetAspect aspect = entityManager.GetAspect<EnemyTargetAspect>(entity);
                    float3 targetPosition = aspect.EnemyTargetComponent.ValueRO.TargetPosition;
                    
                    if (UseTargetMeshes && aspect.EnemyTargetComponent.ValueRO.HasTarget)
                    {
                        targetMeshes[keyValuePair.Key][i].SetTargetPosition(targetPosition);
                    }

                    if (!aspect.CanAttack()) continue;

                    aspect.RestTimer();
                    OriginPosition = aspect.LocalTransform.ValueRO.Position;
                    PerformAttack(targetPosition);
                }
            }
        }
        
        public virtual void RemoveEntities()
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

            foreach (List<DistrictTargetMesh> districtTargetMeshes in targetMeshes.Values)
            {
                foreach (DistrictTargetMesh targetMesh in districtTargetMeshes)
                {
                    targetMesh.gameObject.SetActive(false);
                }
            }
            targetMeshes.Clear();
        }
        
        protected virtual void PerformAttack(float3 targetPosition)
        {
            AttackPosition = targetPosition;
            Attack.TriggerAttack(this);
        }
        
        #endregion
    }
    
    #region Archer

    public sealed class ArcherState : DistrictState
    {
        private readonly TowerData archerData;
        
        private GameObject rangeIndicator;
        
        private bool selected;
        
        public override List<IUpgradeStat> UpgradeStats { get; } = new List<IUpgradeStat>();
        public override Attack Attack { get; }
        
        public override CategoryType CategoryType => CategoryType.Archer;
        protected override bool UseTargetMeshes => true;
        protected override float AttackAngle => 75;

        public ArcherState(DistrictData districtData, TowerData archerData, Vector3 position, int key) : base(districtData, position, key)
        {
            this.archerData = archerData;

            CreateStats();
            SpawnEntities();
            
            Attack = new Attack(archerData.BaseAttack);
        }

        protected override void RangeChanged()
        {
            base.RangeChanged();
            
            if (selected && rangeIndicator && rangeIndicator.activeSelf)
            {
                rangeIndicator.transform.localScale = new Vector3(stats.Range.Value * 2.0f, 0.01f, stats.Range.Value * 2.0f);
            }
        }

        private void CreateStats()
        {
            stats = new Stats(archerData.Stats);
            UpgradeStat healthDamage = new UpgradeStat(stats.HealthDamage, archerData.LevelDatas[0],
                "Health Damage",
                new string[] { "Increase <b>Health Damage Multiplier</b> by {0}", "Current <b>Health Damage Multiplier</b>: <color=green>{0}</color>x" },
                archerData.UpgradeIcons[0]){
                DistrictState = this
            };
            UpgradeStat armorDamage = new UpgradeStat(stats.ArmorDamage, archerData.LevelDatas[1],
                "Armor Damage",
                new string[] { "Increase <b>Armor Damage Multiplier</b> by {0}", "Current <b>Armor Damage Multiplier</b>: <color=yellow>{0}</color>x" },
                archerData.UpgradeIcons[1]){
                DistrictState = this
            };
            UpgradeStat shieldDamage = new UpgradeStat(stats.ShieldDamage, archerData.LevelDatas[2],
                "Shield Damage",
                new string[] { "Increase <b>Shield Damage Multiplier</b> by {0}", "Current <b>Shield Damage Multiplier</b>: <color=blue>{0}</color>x" },
                archerData.UpgradeIcons[2]) {
                DistrictState = this
            };

            UpgradeStats.Add(healthDamage);
            UpgradeStats.Add(armorDamage);
            UpgradeStats.Add(shieldDamage);
        }

        protected override List<QueryChunk> GetEntityChunks(out List<Vector2> offsets, out List<Vector2> directions)
        {
            List<QueryChunk> chunks = new List<QueryChunk>();
            offsets = new List<Vector2>();
            directions = new List<Vector2>();

            foreach (QueryChunk chunk in DistrictData.DistrictChunks.Values)
            {
                for (int i = 0; i < chunk.AdjacentChunks.Length; i++)
                {
                    if (i is 2 or 3 || (chunk.AdjacentChunks[i] != null && chunk.AdjacentChunks[i].PrototypeInfoData == chunk.PrototypeInfoData)) continue;
                    
                    chunks.Add(chunk);
                    Vector2 offset = i switch
                    {
                        0 => new Vector2(0.25f, 0),
                        1 => new Vector2(-0.25f, 0),
                        4 => new Vector2(0, 0.25f),
                        5 => new Vector2(0, -0.25f),
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    offsets.Add(offset);
                    directions.Add(offset.normalized);
                }
            }

            return chunks;
        }
        
        protected override DistrictTargetMesh GetTargetMesh(Vector3 position, Vector2 direction)
        {
            Quaternion rot = Quaternion.LookRotation(direction.ToXyZ(-0.2f).normalized);
            return archerData.DistrictTargetMesh.GetAtPosAndRot<DistrictTargetMesh>(position, rot);
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
    }

    #endregion

    #region Bomb

    public sealed class BombState : DistrictState
    {
        private readonly TowerData bombData;

        private GameObject rangeIndicator;
        private bool selected;
        
        private readonly int2[] corners = // Top left, Top right, bottom left, bottom right 
        {
            new int2(-1, 1),
            new int2(1, 1),
            new int2(-1, -1),
            new int2(1, -1),
        };
        
        public override List<IUpgradeStat> UpgradeStats { get; } = new List<IUpgradeStat>();
        public override CategoryType CategoryType => CategoryType.Bomb;
        protected override bool UseTargetMeshes => true;
        protected override float AttackAngle => 360;
        public override Attack Attack { get; }
        
        public BombState(DistrictData districtData, TowerData bombData, Vector3 position, int key) : base(districtData, position, key)
        {
            this.bombData = bombData;

            CreateStats();
            SpawnEntities();

            Attack = new Attack(bombData.BaseAttack);
        }
        
        protected override List<QueryChunk> GetEntityChunks(out List<Vector2> offsets, out List<Vector2> directions)
        {
            List<QueryChunk> chunks = new List<QueryChunk>();
            HashSet<Tuple<int3, int2>> addedCorners = new HashSet<Tuple<int3, int2>>();
            offsets = new List<Vector2>();
            directions = new List<Vector2>();

            foreach (QueryChunk chunk in DistrictData.DistrictChunks.Values)
            {
                for (int corner = 0; corner < 4; corner++)
                {
                    int3 chunkIndex = chunk.ChunkIndex;
                    int2 cornerDir = corners[corner]; 
                    if (addedCorners.Contains(Tuple.Create(chunkIndex, cornerDir)))
                    {
                        continue;
                    }
                    
                    bool isCornerValid = IsCornerValid(chunkIndex, cornerDir, chunk.PrototypeInfoData);

                    if (!isCornerValid) continue;
                    
                    chunks.Add(chunk);
                    offsets.Add(new Vector2(cornerDir.x, cornerDir.y) * 0.5f);
                    directions.Add(offsets[^1].normalized);
                    AddCornerToHashSet(cornerDir, chunkIndex);
                }
            }

            return chunks;
            
            bool IsCornerValid(int3 chunkIndex, int2 cornerDir, PrototypeInfoData chunkData)
            {
                bool isCornerValid = true;

                for (int x = 0; x < 2 && isCornerValid; x++)
                for (int z = 0; z < 2 && isCornerValid; z++)
                {
                    if (x == 0 && z == 0)
                    {
                        continue;
                    }
                        
                    int3 index = chunkIndex + new int3(x * cornerDir.x, 0, z * cornerDir.y);
                    if (!DistrictData.DistrictChunks.TryGetValue(index, out var chunk) || chunk.PrototypeInfoData != chunkData)
                    {
                        isCornerValid = false;
                    }
                }

                return isCornerValid;
            }
            
            void AddCornerToHashSet(int2 cornerDir, int3 chunkIndex)
            {
                for (int x = 0; x < 2; x++)
                for (int z = 0; z < 2; z++)
                {
                    if (x == 0 && z == 0)
                    {
                        continue;
                    }
                        
                    int3 index = chunkIndex + new int3(x * cornerDir.x, 0, z * cornerDir.y);
                    int2 corner = new int2(cornerDir.x * (x == 1 ? -1 : 1), cornerDir.y * (z == 1 ? -1 : 1));
                    addedCorners.Add(Tuple.Create(index, corner));
                }
            }
        }
        
        protected override DistrictTargetMesh GetTargetMesh(Vector3 position, Vector2 direction)
        {
            return bombData.DistrictTargetMesh.GetAtPosAndRot<DistrictTargetMesh>(position, Quaternion.identity);
        }

        protected override void RangeChanged()
        {
            base.RangeChanged();
            
            if (selected && rangeIndicator is not null && rangeIndicator.activeSelf)
            {
                rangeIndicator.transform.localScale = new Vector3(stats.Range.Value * 2.0f, 0.01f, stats.Range.Value * 2.0f);
            }
        }

        private void CreateStats()
        {
            stats = new Stats(bombData.Stats);
            UpgradeStat healthDamage = new UpgradeStat(stats.HealthDamage, bombData.LevelDatas[0],
                "Health Damage",
                new string[] { "Increase <b>Health Damage Multiplier</b> by {0}", "Current <b>Health Damage Multiplier</b>: <color=green>{0}</color>x" },
                bombData.UpgradeIcons[0]){
                DistrictState = this
            };
            UpgradeStat armorDamage = new UpgradeStat(stats.ArmorDamage, bombData.LevelDatas[1],
                "Armor Damage",
                new string[] { "Increase <b>Armor Damage Multiplier</b> by {0}", "Current <b>Armor Damage Multiplier</b>: <color=yellow>{0}</color>x" },
                bombData.UpgradeIcons[1]){
                DistrictState = this
            };
            UpgradeStat shieldDamage = new UpgradeStat(stats.ShieldDamage, bombData.LevelDatas[2],
                "Shield Damage",
                new string[] { "Increase <b>Shield Damage Multiplier</b> by {0}", "Current <b>Shield Damage Multiplier</b>: <color=blue>{0}</color>x" },
                bombData.UpgradeIcons[2]){
                DistrictState = this
            };

            UpgradeStats.Add(healthDamage);
            UpgradeStats.Add(armorDamage);
            UpgradeStats.Add(shieldDamage);
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
    }
    
    #endregion

    #region Town Hall

    public sealed class TownHallState : DistrictState
    {
        private readonly TowerData townHallData;
        private GameObject rangeIndicator;
        private bool selected;

        public override List<IUpgradeStat> UpgradeStats { get; } = new List<IUpgradeStat>();
        public override CategoryType CategoryType => CategoryType.TownHall;
        protected override bool UseTargetMeshes => false;
        protected override float AttackAngle => 360;
        public override Attack Attack { get; }
        
        public TownHallState(DistrictData districtData, TowerData townHallData, Vector3 position, int key) : base(districtData, position, key)
        {
            this.townHallData = townHallData;

            CreateStats();
            SpawnEntities();
            
            Attack = new Attack(townHallData.BaseAttack);
            Events.OnWaveEnded += OnWaveEnded;
        }

        protected override void RangeChanged()
        {
            base.RangeChanged();
            
            if (selected && rangeIndicator is not null && rangeIndicator.activeSelf)
            {
                rangeIndicator.transform.localScale = new Vector3(stats.Range.Value * 2.0f, 0.01f, stats.Range.Value * 2.0f);
            }
        }

        private void CreateStats()
        {
            stats = new Stats(townHallData.Stats);
            TownHallUpgradeStat townHall = new TownHallUpgradeStat(new Stat(1), townHallData.LevelDatas[0],
                "Level",
                new string[] { "+1 Level - Choose 1 of 2 districts to unlock. ", "Current Level: {0}" },
                townHallData.UpgradeIcons[0]){
                DistrictState = this
            };

            UpgradeStats.Add(townHall);
        }
        
        private void OnWaveEnded()
        {
            
        }

        protected override List<QueryChunk> GetEntityChunks(out List<Vector2> offsets, out List<Vector2> directions)
        {
            offsets = new List<Vector2> { Vector2.zero };
            directions = new List<Vector2> { Vector2.up };
            return new List<QueryChunk> { DistrictData.DistrictChunks.Values.First() };
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

        public override void Dispose()
        {
            Events.OnWaveEnded -= OnWaveEnded;
        }
        
        protected override DistrictTargetMesh GetTargetMesh(Vector3 position, Vector2 offset) => throw new NotImplementedException();
    }

    #endregion
    
    #region Mine

    public sealed class MineState : DistrictState
    {
        private readonly TowerData mineData;
        private GameObject rangeIndicator;

        private bool requireTargeting;
        private bool selected;
        
        public override List<IUpgradeStat> UpgradeStats { get; } = new List<IUpgradeStat>();
        public override CategoryType CategoryType => CategoryType.Mine;
        protected override bool UseTargetMeshes => false;
        protected override float AttackAngle => 360;
        public override Attack Attack { get; }

        public MineState(DistrictData districtData, TowerData mineData, Vector3 position, int key) : base(districtData, position, key)
        {
            this.mineData = mineData;

            CreateStats();
            SpawnEntities();
            Attack = new Attack(mineData.BaseAttack);
            
            Attack.OnEffectsAdded += AttackEffectsAdded;
        }

        protected override void RangeChanged()
        {
            base.RangeChanged();

            if (selected && rangeIndicator is not null && rangeIndicator.activeSelf)
            {
                rangeIndicator.transform.localScale = new Vector3(stats.Range.Value * 2.0f, 0.01f, stats.Range.Value * 2.0f);
            }
        }

        private void AttackEffectsAdded(List<IEffect> effects)
        {
            if (requireTargeting) return;
            if (!DistrictStateUtility.SearchEffects(effects)) return; 
            
            Attack.OnEffectsAdded -= AttackEffectsAdded;
            requireTargeting = true;
        }

        private void CreateStats()
        {
            stats = new Stats(mineData.Stats);
            UpgradeStat attackSpeed = new UpgradeStat(stats.AttackSpeed, mineData.LevelDatas[0], 
                "Mine Speed",
                new string[] { "Increase Mining speed by {0}/s", "Current Mining speed: {0}/s" },
                mineData.UpgradeIcons[0]){
                DistrictState = this
            };
            UpgradeStat damage = new UpgradeStat(stats.Productivity, mineData.LevelDatas[1], 
                "Value Multiplier",
                new string[] { "Increase Value Multiplier by {0}x", "Current Value Multiplier: {0}x" },
                mineData.UpgradeIcons[1]){
                DistrictState = this
            };

            UpgradeStats.Add(attackSpeed);
            UpgradeStats.Add(damage);
        }
        
        public override void OnSelected(Vector3 pos)
        {
            if (!requireTargeting || selected) return;
            
            selected = true;
            rangeIndicator = mineData.RangeIndicator.GetDisabled<PooledMonoBehaviour>().gameObject;
            rangeIndicator.transform.position = pos;
            rangeIndicator.transform.localScale = new Vector3(stats.Range.Value * 2.0f, 0.01f, stats.Range.Value * 2.0f);
            rangeIndicator.gameObject.SetActive(true);
        }

        public override void OnDeselected()
        {
            if (!requireTargeting || rangeIndicator is null) return;
            
            selected = false;
            rangeIndicator.SetActive(false);
            rangeIndicator = null;
        }

        public override void Update()
        {
            if (requireTargeting)
            {
                UpdateEntities();
            }
        }

        public override void OnWaveEnd()
        {
            base.OnWaveEnd();

            foreach (Entity spawnedEntity in spawnedEntities)    
            {
                OriginPosition = entityManager.GetAspect<EnemyTargetAspect>(spawnedEntity).LocalTransform.ValueRO.Position;
                foreach (IEffect effect in mineData.EndWaveEffects)
                {
                    effect.Perform(this);
                }
                
                InvokeStatisticsChanged();
            }
        }

        protected override DistrictTargetMesh GetTargetMesh(Vector3 position, Vector2 offset) => throw new NotImplementedException();
    }

    #endregion

    #region Flame
    public sealed class FlameState : DistrictState
    { 
        private readonly TowerData flameData;

        private GameObject rangeIndicator;
        private bool selected;
        
        private readonly int2[] corners =
        {
            new int2(-1, 1),
            new int2(1, 1),
            new int2(-1, -1),
            new int2(1, -1),
        };
        
        public override List<IUpgradeStat> UpgradeStats { get; } = new List<IUpgradeStat>();
        public override CategoryType CategoryType => CategoryType.Flame;
        protected override bool UseTargetMeshes => true;
        protected override float AttackAngle => 90;
        public override Attack Attack { get; }
        
        public FlameState(DistrictData districtData, TowerData flameData, Vector3 position, int key) : base(districtData, position, key)
        {
            this.flameData = flameData;
 
            CreateStats();
            SpawnEntities();
 
            Attack = new Attack(flameData.BaseAttack);
        }
        
        protected override List<QueryChunk> GetEntityChunks(out List<Vector2> offsets, out List<Vector2> directions)
        {
            List<QueryChunk> chunks = new List<QueryChunk>();
            HashSet<Tuple<int3, int2>> addedCorners = new HashSet<Tuple<int3, int2>>();
            offsets = new List<Vector2>();
            directions = new List<Vector2>();
 
            foreach (QueryChunk chunk in DistrictData.DistrictChunks.Values)
            {
                for (int corner = 0; corner < 4; corner++)
                {
                    int3 chunkIndex = chunk.ChunkIndex;
                    int2 cornerDir = corners[corner]; 
                    if (addedCorners.Contains(Tuple.Create(chunkIndex, cornerDir)))
                    {
                        continue;
                    }
                    
                    bool isCornerValid = IsCornerValid(chunkIndex, cornerDir, chunk.PrototypeInfoData, out Vector2 direction);
 
                    if (!isCornerValid) continue;
                    
                    chunks.Add(chunk);
                    offsets.Add(new Vector2(cornerDir.x, cornerDir.y) * 0.5f);
                    directions.Add(direction);
                    AddCornerToHashSet(cornerDir, chunkIndex);
                }
            }
 
            return chunks;
            
            bool IsCornerValid(int3 chunkIndex, int2 cornerDir, PrototypeInfoData chunkData, out Vector2 direction)
            {
                direction = default;
 
                int3 diagonal = chunkIndex + cornerDir.XyZ(0);
                if (DistrictData.DistrictChunks.TryGetValue(diagonal, out var chunk)
                    && chunk.PrototypeInfoData == chunkData)
                {
                    return false; // The opposite corner should not be built
                }
 
                int3 horizontal = chunkIndex + new int3(cornerDir.x, 0, 0);
                bool horizontalIsValid = DistrictData.DistrictChunks.TryGetValue(horizontal, out var horizontalChunk)
                                         && horizontalChunk.PrototypeInfoData == chunkData;
                
                int3 vertical = chunkIndex + new int3(0, 0, cornerDir.y);
                bool verticalIsValid = DistrictData.DistrictChunks.TryGetValue(vertical, out var verticalChunk)
                                       && verticalChunk.PrototypeInfoData == chunkData;
 
                if (horizontalIsValid == verticalIsValid) // XOR
                {
                    return false;
                }
 
                direction = horizontalIsValid
                    ? new Vector2(0, cornerDir.y)
                    : new Vector2(cornerDir.x, 0);   
                
                return true;
            }
            
            void AddCornerToHashSet(int2 cornerDir, int3 chunkIndex)
            {
                for (int x = 0; x < 2; x++)
                for (int z = 0; z < 2; z++)
                {
                    if (x == 0 && z == 0)
                    {
                        continue;
                    }
                        
                    int3 index = chunkIndex + new int3(x * cornerDir.x, 0, z * cornerDir.y);
                    int2 corner = new int2(cornerDir.x * (x == 1 ? -1 : 1), cornerDir.y * (z == 1 ? -1 : 1));
                    addedCorners.Add(Tuple.Create(index, corner));
                }
            }
        }
       
        protected override DistrictTargetMesh GetTargetMesh(Vector3 position, Vector2 direction)
        {
            Quaternion rot = Quaternion.LookRotation(direction.ToXyZ(-0.2f).normalized);
            return flameData.DistrictTargetMesh.GetAtPosAndRot<DistrictTargetMesh>(position, rot);
        }

        protected override void RangeChanged()
        {
            base.RangeChanged();

            if (selected && rangeIndicator is not null && rangeIndicator.activeSelf)
            {
                rangeIndicator.transform.localScale = new Vector3(stats.Range.Value * 2.0f, 0.01f, stats.Range.Value * 2.0f);
            }
        }
 
        private void CreateStats()
        {
            stats = new Stats(flameData.Stats);
            UpgradeStat healthDamage = new UpgradeStat(stats.HealthDamage, flameData.LevelDatas[0],
                "Health Damage",
                new string[] { "Increase <b>Health Damage Multiplier</b> by {0}", "Current <b>Health Damage Multiplier</b>: <color=green>{0}</color>x" },
                flameData.UpgradeIcons[0]){
                DistrictState = this
            };
            UpgradeStat armorDamage = new UpgradeStat(stats.ArmorDamage, flameData.LevelDatas[1],
                "Armor Damage",
                new string[] { "Increase <b>Armor Damage Multiplier</b> by {0}", "Current <b>Armor Damage Multiplier</b>: <color=yellow>{0}</color>x" },
                flameData.UpgradeIcons[1]){
                DistrictState = this
            };
            UpgradeStat shieldDamage = new UpgradeStat(stats.ShieldDamage, flameData.LevelDatas[2],
                "Shield Damage",
                new string[] { "Increase <b>Shield Damage Multiplier</b> by {0}", "Current <b>Shield Damage Multiplier</b>: <color=blue>{0}</color>x" },
                flameData.UpgradeIcons[2]){
                DistrictState = this
            };
 
            UpgradeStats.Add(healthDamage);
            UpgradeStats.Add(armorDamage);
            UpgradeStats.Add(shieldDamage);
        }
        
        public override void OnSelected(Vector3 pos)
        {
            if (selected) return;
            
            selected = true;
            rangeIndicator = flameData.RangeIndicator.GetDisabled<PooledMonoBehaviour>().gameObject;
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
    }
     
     #endregion
     
    #region Lightning
     
    public sealed class LightningState : DistrictState
    { 
        private readonly TowerData lightningData;

        private GameObject rangeIndicator;
        private bool selected;
        
        public override List<IUpgradeStat> UpgradeStats { get; } = new List<IUpgradeStat>();
        public override CategoryType CategoryType => CategoryType.Lightning;
        protected override bool UseTargetMeshes => false;
        protected override float AttackAngle => 360;
        public override Attack Attack { get; }
        
        public LightningState(DistrictData districtData, TowerData lightningData, Vector3 position, int key) : base(districtData, position, key)
        {
            this.lightningData = lightningData;

            CreateStats();
            SpawnEntities();

            Attack = new Attack(lightningData.BaseAttack);
        }
        
        protected override List<QueryChunk> GetEntityChunks(out List<Vector2> offsets, out List<Vector2> directions)
        {
            List<QueryChunk> chunks = new List<QueryChunk>();
            offsets = new List<Vector2>();
            directions = new List<Vector2>();

            foreach (QueryChunk chunk in DistrictData.DistrictChunks.Values)
            {
                chunks.Add(chunk);
                offsets.Add(DistrictData.DistrictGenerator.ChunkScale / 2.0f);
                directions.Add(Vector2.zero);
            }

            return chunks;
        }

        protected override void RangeChanged()
        {
            base.RangeChanged();

            if (selected && rangeIndicator is not null && rangeIndicator.activeSelf)
            {
                rangeIndicator.transform.localScale = new Vector3(stats.Range.Value * 2.0f, 0.01f, stats.Range.Value * 2.0f);
            }
        }

        private void CreateStats()
        {
            stats = new Stats(lightningData.Stats);
            UpgradeStat healthDamage = new UpgradeStat(stats.HealthDamage, lightningData.LevelDatas[0],
                "Health Damage",
                new string[] { "Increase <b>Health Damage Multiplier</b> by {0}", "Current <b>Health Damage Multiplier</b>: <color=green>{0}</color>x" },
                lightningData.UpgradeIcons[0]){
                DistrictState = this
            };
            UpgradeStat armorDamage = new UpgradeStat(stats.ArmorDamage, lightningData.LevelDatas[1],
                "Armor Damage",
                new string[] { "Increase <b>Armor Damage Multiplier</b> by {0}", "Current <b>Armor Damage Multiplier</b>: <color=yellow>{0}</color>x" },
                lightningData.UpgradeIcons[1]){
                DistrictState = this
            };
            UpgradeStat shieldDamage = new UpgradeStat(stats.ShieldDamage, lightningData.LevelDatas[2],
                "Shield Damage",
                new string[] { "Increase <b>Shield Damage Multiplier</b> by {0}", "Current <b>Shield Damage Multiplier</b>: <color=blue>{0}</color>x" },
                lightningData.UpgradeIcons[2]){
                DistrictState = this
            };

            UpgradeStats.Add(healthDamage);
            UpgradeStats.Add(armorDamage);
            UpgradeStats.Add(shieldDamage);
        }
        
        public override void OnSelected(Vector3 pos)
        {
            if (selected) return;
            
            selected = true;
            rangeIndicator = lightningData.RangeIndicator.GetDisabled<PooledMonoBehaviour>().gameObject;
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
    
        protected override DistrictTargetMesh GetTargetMesh(Vector3 position, Vector2 direction) => throw new NotImplementedException();
    }
     
     #endregion
     
    #region Church

    public sealed class ChurchState : DistrictState
    {
        private readonly TowerData churchData;
        private GameObject rangeIndicator;

        private bool requireTargeting;
        private bool selected;
        
        private readonly int2[] corners = // Top left, Top right, bottom left, bottom right 
        {
            new int2(-1, 1),
            new int2(1, 1),
            new int2(-1, -1),
            new int2(1, -1),
        };
        
        public override List<IUpgradeStat> UpgradeStats { get; } = new List<IUpgradeStat>();
        public override CategoryType CategoryType => CategoryType.Church;
        protected override bool UseTargetMeshes => true;
        protected override float AttackAngle => 360;
        public override Attack Attack { get; }

        public ChurchState(DistrictData districtData, TowerData churchData, Vector3 position, int key) : base(districtData, position, key)
        {
            this.churchData = churchData;

            CreateStats(); 
            SpawnEntities(); 
            Attack = new Attack(churchData.BaseAttack);
            
            Attack.OnEffectsAdded += AttackEffectsAdded;
            
            Stats.Productivity.OnValueChanged += OnStatsChanged;
        }

        private void OnStatsChanged()
        {
            RevertCreateEffects();
            PerformCreateEffects();
        }

        protected override void RangeChanged()
        {
            base.RangeChanged();
            OnStatsChanged();
            
            if (selected && rangeIndicator is not null && rangeIndicator.activeSelf)
            {
                rangeIndicator.transform.localScale = new Vector3(stats.Range.Value * 2.0f, 0.01f, stats.Range.Value * 2.0f);
            }
        }

        protected override List<QueryChunk> GetEntityChunks(out List<Vector2> offsets, out List<Vector2> directions)
        {
            List<QueryChunk> chunks = new List<QueryChunk>();
            HashSet<Tuple<int3, int2>> addedCorners = new HashSet<Tuple<int3, int2>>();
            offsets = new List<Vector2>();
            directions = new List<Vector2>();

            foreach (QueryChunk chunk in DistrictData.DistrictChunks.Values)
            {
                for (int corner = 0; corner < 4; corner++)
                {
                    int3 chunkIndex = chunk.ChunkIndex;
                    int2 cornerDir = corners[corner]; 
                    if (addedCorners.Contains(Tuple.Create(chunkIndex, cornerDir)))
                    {
                        continue;
                    }
                    
                    bool isCornerValid = IsCornerValid(chunkIndex, cornerDir, chunk.PrototypeInfoData);

                    if (!isCornerValid) continue;
                    
                    chunks.Add(chunk);
                    offsets.Add(new Vector2(cornerDir.x, cornerDir.y) * 0.5f);
                    directions.Add(offsets[^1].normalized);
                    AddCornerToHashSet(cornerDir, chunkIndex);
                }
            }

            return chunks;
            
            bool IsCornerValid(int3 chunkIndex, int2 cornerDir, PrototypeInfoData chunkData)
            {
                bool isCornerValid = true;

                for (int x = 0; x < 2 && isCornerValid; x++)
                for (int z = 0; z < 2 && isCornerValid; z++)
                {
                    if (x == 0 && z == 0)
                    {
                        continue;
                    }
                        
                    int3 index = chunkIndex + new int3(x * cornerDir.x, 0, z * cornerDir.y);
                    if (!DistrictData.DistrictChunks.TryGetValue(index, out var chunk) || chunk.PrototypeInfoData != chunkData)
                    {
                        isCornerValid = false;
                    }
                }

                return isCornerValid;
            }
            
            void AddCornerToHashSet(int2 cornerDir, int3 chunkIndex)
            {
                for (int x = 0; x < 2; x++)
                for (int z = 0; z < 2; z++)
                {
                    if (x == 0 && z == 0)
                    {
                        continue;
                    }
                        
                    int3 index = chunkIndex + new int3(x * cornerDir.x, 0, z * cornerDir.y);
                    int2 corner = new int2(cornerDir.x * (x == 1 ? -1 : 1), cornerDir.y * (z == 1 ? -1 : 1));
                    addedCorners.Add(Tuple.Create(index, corner));
                }
            }
        }
        
        protected override DistrictTargetMesh GetTargetMesh(Vector3 position, Vector2 direction)
        {
            return churchData.DistrictTargetMesh.GetAtPosAndRot<DistrictTargetMesh>(position, Quaternion.identity);
        }
        
        public override void SpawnEntities()
        {
            base.SpawnEntities();

            PerformCreateEffects();
        }

        public override void RemoveEntities()
        {
            base.RemoveEntities();

            RevertCreateEffects();
        }
        
        private void PerformCreateEffects()
        {
            foreach (Entity entity in spawnedEntities)   
            {
                float3 pos = entityManager.GetComponentData<LocalTransform>(entity).Position;
                OriginPosition = pos;
                foreach (IEffect createdEffect in churchData.CreatedEffects)
                {
                    createdEffect.Perform(this);
                }
            }
        }
        
        private void RevertCreateEffects()
        {
            foreach (IEffect createdEffect in churchData.CreatedEffects)
            {
                createdEffect.Revert(this);
            }
        }

        private void AttackEffectsAdded(List<IEffect> effects)
        {
            if (requireTargeting) return;
            if (!SearchEffects(effects)) return; 
            
            Attack.OnEffectsAdded -= AttackEffectsAdded;
            requireTargeting = true;
        }

        private bool SearchEffects(List<IEffect> effects)
        {
            foreach (IEffect effect in effects)
            {
                if (effect.IsDamageEffect)
                {
                    return true;
                }
                
                if (effect is IEffectHolder holder && SearchEffects(holder.Effects))
                {
                    return true;
                }
            }

            return false;
        }

        private void CreateStats()
        {
            stats = new Stats(churchData.Stats);
            
            UpgradeStat buffPower = new UpgradeStat(stats.Productivity, churchData.LevelDatas[0], 
                "Buff Power",
                new string[] { "Increase Buff Power by {0}", "Current Buff Power: {0}" },
                churchData.UpgradeIcons[0]){
                DistrictState = this
            };

            UpgradeStats.Add(buffPower);
        }
        
        public override void OnSelected(Vector3 pos)
        {
            if (selected) return;
            
            selected = true;
            rangeIndicator = churchData.RangeIndicator.GetDisabled<PooledMonoBehaviour>().gameObject;
            rangeIndicator.transform.position = pos;
            rangeIndicator.transform.localScale = new Vector3(stats.Range.Value * 2.0f, 0.01f, stats.Range.Value * 2.0f);
            rangeIndicator.gameObject.SetActive(true);
        }

        public override void OnDeselected()
        {
            if (rangeIndicator is null) return;
            
            selected = false;
            rangeIndicator.SetActive(false);
            rangeIndicator = null;
        }

        public override void Update()
        {
            if (requireTargeting)
            {
                UpdateEntities();
            }
        }
    }

    #endregion

    #region Barracks

    public sealed class BarracksState : DistrictState
    {
        private readonly TowerData barracksData;
        private GameObject rangeIndicator;

        private bool selected;
        private bool requireTargeting;

        public override List<IUpgradeStat> UpgradeStats { get; } = new List<IUpgradeStat>();
        public override Attack Attack { get; }
        
        public override CategoryType CategoryType => CategoryType.Barracks;
        protected override bool UseTargetMeshes => false;
        protected override float AttackAngle => 360;

        public BarracksState(DistrictData districtData, TowerData data, Vector3 position, int key) : base(districtData, position, key)
        {
            barracksData = data;

            CreateStats();
            SpawnEntities();
            
            Attack = new Attack(barracksData.BaseAttack);
            Attack.OnEffectsAdded += AttackEffectsAdded;
        }

        private void AttackEffectsAdded(List<IEffect> effects)
        {
            if (requireTargeting) return;
            if (!DistrictStateUtility.SearchEffects(effects)) return; 
            
            Attack.OnEffectsAdded -= AttackEffectsAdded;
            requireTargeting = true;
        }

        private void CreateStats()
        {
            stats = new Stats(barracksData.Stats);
        }

        protected override List<QueryChunk> GetEntityChunks(out List<Vector2> offsets, out List<Vector2> directions)
        {
            List<QueryChunk> chunks = new List<QueryChunk>();
            offsets = new List<Vector2>();
            directions = new List<Vector2>();

            foreach (QueryChunk chunk in DistrictData.DistrictChunks.Values)
            {
                for (int i = 0; i < chunk.AdjacentChunks.Length; i++)
                {
                    if (i is 2 or 3 || (chunk.AdjacentChunks[i] != null && chunk.AdjacentChunks[i].PrototypeInfoData == chunk.PrototypeInfoData)) continue;
                    
                    chunks.Add(chunk);
                    Vector2 offset = i switch
                    {
                        0 => new Vector2(0.25f, 0),
                        1 => new Vector2(-0.25f, 0),
                        4 => new Vector2(0, 0.25f),
                        5 => new Vector2(0, -0.25f),
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    offsets.Add(offset);
                    directions.Add(offset.normalized);
                }
            }

            return chunks;
        }
        
        public override void OnSelected(Vector3 pos)
        {
            if (!requireTargeting || selected) return;
            
            selected = true;
            rangeIndicator = barracksData.RangeIndicator.GetDisabled<PooledMonoBehaviour>().gameObject;
            rangeIndicator.transform.position = pos;
            rangeIndicator.transform.localScale = new Vector3(stats.Range.Value * 2.0f, 0.01f, stats.Range.Value * 2.0f);
            rangeIndicator.gameObject.SetActive(true);
        }

        public override void OnDeselected()
        {
            if (!requireTargeting || rangeIndicator is null) return;
            
            selected = false;
            rangeIndicator.SetActive(false);
            rangeIndicator = null;
        }


        public override void Update()
        {
            if (requireTargeting)
            {
                UpdateEntities();
            }
        }

        protected override DistrictTargetMesh GetTargetMesh(Vector3 position, Vector2 direction) => throw new NotImplementedException();
    }

    #endregion

    public static class DistrictStateUtility
    {
        public static bool SearchEffects(List<IEffect> effects)
        {
            foreach (IEffect effect in effects)
            {
                if (effect.IsDamageEffect)
                {
                    return true;
                }
                
                if (effect is IEffectHolder holder && SearchEffects(holder.Effects))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

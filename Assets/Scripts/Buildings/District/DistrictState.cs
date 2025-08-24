using LocalToWorld = Unity.Transforms.LocalToWorld;
using Vector2 = UnityEngine.Vector2;
using System.Collections.Generic;
using Buildings.District.ECS;
using UnityEngine.Rendering;
using WaveFunctionCollapse;
using Unity.Collections;
using Unity.Mathematics;
using Gameplay.Upgrades;
using Unity.Transforms;
using Unity.Rendering;
using Unity.Entities;
using System.Linq;
using Effects.ECS;
using UnityEngine;
using Enemy.ECS;
using Effects;
using System;

namespace Buildings.District
{
    public abstract class DistrictState : IAttacker, IAttackerStatistics, IDisposable
    {
        public event Action OnStatisticsChanged;
        public event Action OnAttack;

        private readonly Dictionary<int2, List<Entity>> entityIndexes = new Dictionary<int2, List<Entity>>();
        protected readonly HashSet<Entity> spawnedDataEntities = new HashSet<Entity>();
        protected readonly HashSet<Entity> allSpawnedEntities = new HashSet<Entity>();
        
        protected DamageInstance lastDamageDone;
        protected EntityManager entityManager;
        protected TowerData districtData;
        protected Stats stats;

        private Entity simpleTargetingEntityPrefab;
        private Entity targetingEntityPrefab;
        private Entity targetEntityPrefab;
        
        protected bool requireTargeting;
        
        public DamageInstance LastDamageDone => lastDamageDone;
        public Stats Stats => stats;

        public abstract List<IUpgradeStat> UpgradeStats { get; }
        protected DistrictData DistrictData { get; }
        public Vector3 OriginPosition { get; set; }
        public Vector3 AttackPosition { get; set; }
        public abstract Attack Attack { get; }
        public float DamageDone { get; set; }
        public float GoldGained { get; set; }
        public int Level { get; set; } = 1;
        public int Key { get; set; }
        
        public CategoryType CategoryType => districtData.CategoryType;

        protected DistrictState(DistrictData districtData, Vector3 position, int key, TowerData towerData)
        {
            this.districtData = towerData;
            requireTargeting = towerData.RequireTargeting;
            
            DistrictData = districtData;
            OriginPosition = position;

            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            Key = key;
            
            DamageCallbackHandler.DamageDoneEvent.Add(key, OnDamageDone);
            
            CreatePrefabEntities();
        }

        private void CreatePrefabEntities()
        {
            ComponentType[] targetingComponentTypes =
            {
                typeof(DirectionRangeComponent),
                typeof(DistrictDataComponent),
                typeof(EnemyTargetComponent),
                typeof(AttackSpeedComponent),
                typeof(LocalTransform),
                typeof(RangeComponent),
                typeof(Prefab),
            };
            targetingEntityPrefab = entityManager.CreateEntity(targetingComponentTypes);
            
            ComponentType[] simpleComponentTypes =
            {
                typeof(DistrictDataComponent),
                typeof(AttackSpeedComponent),
                typeof(LocalTransform),
                typeof(Prefab),
            };
            simpleTargetingEntityPrefab = entityManager.CreateEntity(simpleComponentTypes);

            ComponentType[] targetComponents =
            {
                typeof(TargetMeshComponent),
                typeof(LocalTransform),
                typeof(LocalToWorld),
                typeof(Prefab),
            };
            targetEntityPrefab = entityManager.CreateEntity(targetComponents);
        }

        public virtual void Update(){}
        public abstract void OnSelected(Vector3 pos);
        public abstract void OnDeselected();

        public void OnIndexesDestroyed(HashSet<int3> destroyedIndexes)
        {
            foreach (int3 destroyedIndex in destroyedIndexes)
            {
                DistrictData.DistrictChunks.Remove(destroyedIndex);
            }
        }

        public virtual void OnWaveStart() { }
        
        public virtual void OnWaveEnd() { }
        
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

        public virtual void OnUnitKill() { }

        protected void InvokeStatisticsChanged()
        {
            OnStatisticsChanged?.Invoke();
        }
        
        public virtual void Dispose()
        {
            DamageCallbackHandler.DamageDoneEvent.Remove(Key);
            
            entityManager.DestroyEntity(simpleTargetingEntityPrefab);
            entityManager.DestroyEntity(targetingEntityPrefab);
            entityManager.DestroyEntity(targetEntityPrefab);
            
            RemoveEntities();
        }

        #region Entities

        protected virtual List<QueryChunk> GetEntityChunks(out List<Vector2> offsets, out List<Vector2> directions)
        {
            offsets = new Vector2[DistrictData.DistrictChunks.Values.Count].ToList();
            directions = new Vector2[DistrictData.DistrictChunks.Values.Count].ToList();
            return DistrictData.DistrictChunks.Values.ToList();
        }

        protected void SpawnEntitiesWithoutTargeting()
        {
            List<QueryChunk> topChunks = GetEntityChunks(out List<Vector2> offsets, out _);
            
            int count = topChunks.Count;
            float attackSpeedValue = 1.0f / stats.AttackSpeed.Value;
            float delay = attackSpeedValue / count;
            
            NativeArray<Entity> entities = entityManager.Instantiate(simpleTargetingEntityPrefab, count, Allocator.Temp);
            
            for (int i = 0; i < count; i++)
            {
                SetupSimpleTargetEntity(entities[i], i);
            }
            
            entities.Dispose();
            stats.Range.OnValueChanged += RangeChanged;
            stats.AttackSpeed.OnValueChanged += AttackSpeedChanged;

            void SetupSimpleTargetEntity(Entity spawnedEntity, int i)
            {
                Vector3 offset = DistrictData.DistrictGenerator.ChunkScale.MultiplyByAxis(new Vector3(0.5f + offsets[i].x, 0.5f, 0.5f + offsets[i].y));
                Vector3 pos = topChunks[i].Position + offset;

                entityManager.SetComponentData(spawnedEntity, new LocalTransform { Position = pos });
                entityManager.SetComponentData(spawnedEntity, new AttackSpeedComponent
                {
                    AttackSpeed = attackSpeedValue, 
                    Timer = delay * i
                });

                int2 key = topChunks[i].ChunkIndex.xz;
                AddEntityToDictionary(key, spawnedEntity, true);
                
                entityManager.SetComponentData(spawnedEntity, new DistrictDataComponent
                {
                    DistrictID = Key,
                });
            }
        }
        
        public virtual void SpawnEntities()
        {
            if (!requireTargeting)
            {
                SpawnEntitiesWithoutTargeting();
                return;
            }
            
            List<QueryChunk> topChunks = GetEntityChunks(out List<Vector2> offsets, out List<Vector2> directions);
            
            int count = topChunks.Count;
            float attackSpeedValue = 1.0f / stats.AttackSpeed.Value;
            float delay = attackSpeedValue / count;
            
            NativeArray<Entity> entities = entityManager.Instantiate(targetingEntityPrefab, count, Allocator.Temp);
            
            for (int i = 0; i < count; i++)
            {
                SetupTargetEntity(entities[i], i);
            }
            
            if (districtData.UseTargetMesh)
            {
                SpawnTargetMeshEntities(entities, count, topChunks, offsets, directions);
            }
            
            entities.Dispose();
            stats.Range.OnValueChanged += RangeChanged;
            stats.AttackSpeed.OnValueChanged += AttackSpeedChanged;

            void SetupTargetEntity(Entity spawnedEntity, int i)
            {
                entityManager.SetComponentData(spawnedEntity, new RangeComponent { Range = stats.Range.Value });

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
                    Angle = districtData.AttackAngle,
                });
                
                entityManager.SetComponentData(spawnedEntity, new DistrictDataComponent
                {
                    DistrictID = Key,
                });
                
                spawnedDataEntities.Add(spawnedEntity);

                int2 key = topChunks[i].ChunkIndex.xz;
                AddEntityToDictionary(key, spawnedEntity, true);
            }
        }

        private void SpawnTargetMeshEntities(NativeArray<Entity> entities, int count, List<QueryChunk> topChunks, List<Vector2> offsets, List<Vector2> directions)
        {
            RenderMeshDescription desc = new RenderMeshDescription(shadowCastingMode: ShadowCastingMode.On, receiveShadows: true);
            RenderMeshArray renderMeshArray = new RenderMeshArray(new Material[] { districtData.MeshVariable.Material }, new Mesh[] { districtData.MeshVariable.Mesh });
            RenderMeshUtility.AddComponents(targetEntityPrefab, entityManager, desc, renderMeshArray, MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));
            
            NativeArray<Entity> targetEntities = entityManager.Instantiate(targetEntityPrefab, count, Allocator.Temp);

            for (int i = 0; i < count; i++)
            {
                SetupTargetEntity(targetEntities[i], i);
            }
            
            targetEntities.Dispose();

            void SetupTargetEntity(Entity targetEntity, int i)
            {
                Vector3 offset = DistrictData.DistrictGenerator.ChunkScale.MultiplyByAxis(new Vector3(0.5f + offsets[i].x, 0.5f, 0.5f + offsets[i].y));
                Vector3 pos = topChunks[i].Position + offset;
                
                entityManager.SetComponentData(targetEntity, new TargetMeshComponent
                {
                    Target = entities[i],
                });
                
                entityManager.SetComponentData(targetEntity, new LocalTransform
                {
                    Position = pos,
                    Rotation = quaternion.LookRotation(directions[i].ToXyZ(0), Vector3.up),
                    Scale = districtData.MeshVariable.Scale
                });
                
                int2 key = topChunks[i].ChunkIndex.xz;
                AddEntityToDictionary(key, targetEntity, false);
            }
        }

        private void AddEntityToDictionary(int2 key, Entity spawnedEntity, bool isDataEntity)
        {
            if (isDataEntity)
            {
                spawnedDataEntities.Add(spawnedEntity);
            }
            
            allSpawnedEntities.Add(spawnedEntity);
            if (entityIndexes.TryGetValue(key, out List<Entity> list)) list.Add(spawnedEntity);
            else entityIndexes.Add(key, new List<Entity> { spawnedEntity });
        }

        protected virtual void AttackSpeedChanged()
        {
            float attackSpeedValue = 1.0f / stats.AttackSpeed.Value;
            float delay = attackSpeedValue / entityIndexes.Count;
            int index = 0;
            foreach (Entity entity in spawnedDataEntities)
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
            foreach (Entity entity in spawnedDataEntities)
            {
                entityManager.SetComponentData(entity, new RangeComponent { Range = stats.Range.Value });
            }
        }
        
        public virtual void RemoveEntities()
        {
            NativeArray<Entity> entitiesToDestroy = new NativeArray<Entity>(allSpawnedEntities.Count, Allocator.Temp);
            int index = 0;
            foreach (Entity entity in allSpawnedEntities)
            {
                entitiesToDestroy[index++] = entity;
            }
            
            entityManager.DestroyEntity(entitiesToDestroy);
            
            entityIndexes.Clear();
            spawnedDataEntities.Clear();
            allSpawnedEntities.Clear();
            entitiesToDestroy.Dispose();
        }
        
        public virtual void PerformAttack(float3 targetPosition)
        {
            AttackPosition = targetPosition;
            Attack.TriggerAttack(this);
        }
        
        #endregion
    }
    
    #region Archer

    public sealed class ArcherState : DistrictState
    {
        private GameObject rangeIndicator;
        
        private bool selected;
        
        public override List<IUpgradeStat> UpgradeStats { get; } = new List<IUpgradeStat>();
        public override Attack Attack { get; }
        

        public ArcherState(DistrictData districtData, TowerData archerData, Vector3 position, int key) : base(districtData, position, key, archerData)
        {
            CreateStats();
            SpawnEntities();
            
            Attack = new Attack(this.districtData.BaseAttack);
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
            stats = new Stats(districtData.Stats);
            UpgradeStat healthDamage = new UpgradeStat(stats.HealthDamage, districtData.LevelDatas[0],
                "Health Damage",
                new string[] { "Increase <b>Health Damage Multiplier</b> by {0}", "Current <b>Health Damage Multiplier</b>: <color=green>{0}</color>x" },
                districtData.UpgradeIcons[0]){
                DistrictState = this
            };
            UpgradeStat armorDamage = new UpgradeStat(stats.ArmorDamage, districtData.LevelDatas[1],
                "Armor Damage",
                new string[] { "Increase <b>Armor Damage Multiplier</b> by {0}", "Current <b>Armor Damage Multiplier</b>: <color=yellow>{0}</color>x" },
                districtData.UpgradeIcons[1]){
                DistrictState = this
            };
            UpgradeStat shieldDamage = new UpgradeStat(stats.ShieldDamage, districtData.LevelDatas[2],
                "Shield Damage",
                new string[] { "Increase <b>Shield Damage Multiplier</b> by {0}", "Current <b>Shield Damage Multiplier</b>: <color=blue>{0}</color>x" },
                districtData.UpgradeIcons[2]) {
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

        public override void OnSelected(Vector3 pos)
        {
            if (selected) return;
            
            selected = true;
            rangeIndicator = districtData.RangeIndicator.GetDisabled<PooledMonoBehaviour>().gameObject;
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
    }

    #endregion

    #region Bomb

    public sealed class BombState : DistrictState
    {
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
        public override Attack Attack { get; }
        
        public BombState(DistrictData districtData, TowerData bombData, Vector3 position, int key) : base(districtData, position, key, bombData)
        {
            CreateStats();
            SpawnEntities();

            Attack = new Attack(this.districtData.BaseAttack);
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
            stats = new Stats(districtData.Stats);
            UpgradeStat healthDamage = new UpgradeStat(stats.HealthDamage, districtData.LevelDatas[0],
                "Health Damage",
                new string[] { "Increase <b>Health Damage Multiplier</b> by {0}", "Current <b>Health Damage Multiplier</b>: <color=green>{0}</color>x" },
                districtData.UpgradeIcons[0]){
                DistrictState = this
            };
            UpgradeStat armorDamage = new UpgradeStat(stats.ArmorDamage, districtData.LevelDatas[1],
                "Armor Damage",
                new string[] { "Increase <b>Armor Damage Multiplier</b> by {0}", "Current <b>Armor Damage Multiplier</b>: <color=yellow>{0}</color>x" },
                districtData.UpgradeIcons[1]){
                DistrictState = this
            };
            UpgradeStat shieldDamage = new UpgradeStat(stats.ShieldDamage, districtData.LevelDatas[2],
                "Shield Damage",
                new string[] { "Increase <b>Shield Damage Multiplier</b> by {0}", "Current <b>Shield Damage Multiplier</b>: <color=blue>{0}</color>x" },
                districtData.UpgradeIcons[2]){
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
            rangeIndicator = districtData.RangeIndicator.GetDisabled<PooledMonoBehaviour>().gameObject;
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
    }
    
    #endregion

    #region Town Hall

    public sealed class TownHallState : DistrictState
    {
        private GameObject rangeIndicator;
        private bool selected;

        public override List<IUpgradeStat> UpgradeStats { get; } = new List<IUpgradeStat>();
        public override Attack Attack { get; }
        
        public TownHallState(DistrictData districtData, TowerData townhallData, Vector3 position, int key) : base(districtData, position, key, townhallData)
        {
            CreateStats();
            SpawnEntities();
            
            Attack = new Attack(townhallData.BaseAttack);
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
            stats = new Stats(districtData.Stats);
            TownHallUpgradeStat townHall = new TownHallUpgradeStat(new Stat(1), districtData.LevelDatas[0],
                "Level",
                new string[] { "+1 Level - Choose 1 of 2 districts to unlock. ", "Current Level: {0}" },
                districtData.UpgradeIcons[0]){
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

        public override void OnSelected(Vector3 pos)
        {
            if (selected) return;
            
            selected = true;
            rangeIndicator = districtData.RangeIndicator.GetDisabled<PooledMonoBehaviour>().gameObject;
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
    }

    #endregion
    
    #region Mine

    public sealed class MineState : DistrictState
    {
        private GameObject rangeIndicator;
        
        private bool selected;
        
        public override List<IUpgradeStat> UpgradeStats { get; } = new List<IUpgradeStat>();
        public override Attack Attack { get; }

        public MineState(DistrictData districtData, TowerData mineData, Vector3 position, int key) : base(districtData, position, key, mineData)
        {
            this.districtData = mineData;

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
            if (requireTargeting)
            {
                Attack.OnEffectsAdded -= AttackEffectsAdded;
                return;
            }
            
            if (!DistrictStateUtility.SearchEffects(effects)) return; 
            
            Attack.OnEffectsAdded -= AttackEffectsAdded;
            requireTargeting = true;
            
            RemoveEntities();
            SpawnEntities();
        }

        private void CreateStats()
        {
            stats = new Stats(districtData.Stats);
            UpgradeStat attackSpeed = new UpgradeStat(stats.AttackSpeed, districtData.LevelDatas[0], 
                "Mine Speed",
                new string[] { "Increase Mining speed by {0}/s", "Current Mining speed: {0}/s" },
                districtData.UpgradeIcons[0]){
                DistrictState = this
            };
            UpgradeStat damage = new UpgradeStat(stats.Productivity, districtData.LevelDatas[1], 
                "Value Multiplier",
                new string[] { "Increase Value Multiplier by {0}x", "Current Value Multiplier: {0}x" },
                districtData.UpgradeIcons[1]){
                DistrictState = this
            };

            UpgradeStats.Add(attackSpeed);
            UpgradeStats.Add(damage);
        }
        
        public override void OnSelected(Vector3 pos)
        {
            if (!requireTargeting || selected) return;
            
            selected = true;
            rangeIndicator = districtData.RangeIndicator.GetDisabled<PooledMonoBehaviour>().gameObject;
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
        
        public override void OnWaveEnd()
        {
            base.OnWaveEnd();

            foreach (QueryChunk chunk in DistrictData.DistrictChunks.Values)
            {
                OriginPosition = chunk.Position;
                foreach (IEffect effect in districtData.EndWaveEffects)
                {
                    effect.Perform(this);
                }
                
                InvokeStatisticsChanged();
            }
        }
    }

    #endregion

    #region Flame
    public sealed class FlameState : DistrictState
    { 
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
        public override Attack Attack { get; }
        
        public FlameState(DistrictData districtData, TowerData flameData, Vector3 position, int key) : base(districtData, position, key, flameData)
        {
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
            stats = new Stats(districtData.Stats);
            UpgradeStat healthDamage = new UpgradeStat(stats.HealthDamage, districtData.LevelDatas[0],
                "Health Damage",
                new string[] { "Increase <b>Health Damage Multiplier</b> by {0}", "Current <b>Health Damage Multiplier</b>: <color=green>{0}</color>x" },
                districtData.UpgradeIcons[0]){
                DistrictState = this
            };
            UpgradeStat armorDamage = new UpgradeStat(stats.ArmorDamage, districtData.LevelDatas[1],
                "Armor Damage",
                new string[] { "Increase <b>Armor Damage Multiplier</b> by {0}", "Current <b>Armor Damage Multiplier</b>: <color=yellow>{0}</color>x" },
                districtData.UpgradeIcons[1]){
                DistrictState = this
            };
            UpgradeStat shieldDamage = new UpgradeStat(stats.ShieldDamage, districtData.LevelDatas[2],
                "Shield Damage",
                new string[] { "Increase <b>Shield Damage Multiplier</b> by {0}", "Current <b>Shield Damage Multiplier</b>: <color=blue>{0}</color>x" },
                districtData.UpgradeIcons[2]){
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
            rangeIndicator = districtData.RangeIndicator.GetDisabled<PooledMonoBehaviour>().gameObject;
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
    }
     
     #endregion
     
    #region Lightning
     
    public sealed class LightningState : DistrictState
    { 
        private GameObject rangeIndicator;
        private bool selected;
        
        public override List<IUpgradeStat> UpgradeStats { get; } = new List<IUpgradeStat>();
        public override Attack Attack { get; }
        
        public LightningState(DistrictData districtData, TowerData flameData, Vector3 position, int key) : base(districtData, position, key, flameData)
        {
            CreateStats();
            SpawnEntities();

            Attack = new Attack(flameData.BaseAttack);
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
            stats = new Stats(districtData.Stats);
            UpgradeStat healthDamage = new UpgradeStat(stats.HealthDamage, districtData.LevelDatas[0],
                "Health Damage",
                new string[] { "Increase <b>Health Damage Multiplier</b> by {0}", "Current <b>Health Damage Multiplier</b>: <color=green>{0}</color>x" },
                districtData.UpgradeIcons[0]){
                DistrictState = this
            };
            UpgradeStat armorDamage = new UpgradeStat(stats.ArmorDamage, districtData.LevelDatas[1],
                "Armor Damage",
                new string[] { "Increase <b>Armor Damage Multiplier</b> by {0}", "Current <b>Armor Damage Multiplier</b>: <color=yellow>{0}</color>x" },
                districtData.UpgradeIcons[1]){
                DistrictState = this
            };
            UpgradeStat shieldDamage = new UpgradeStat(stats.ShieldDamage, districtData.LevelDatas[2],
                "Shield Damage",
                new string[] { "Increase <b>Shield Damage Multiplier</b> by {0}", "Current <b>Shield Damage Multiplier</b>: <color=blue>{0}</color>x" },
                districtData.UpgradeIcons[2]){
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
            rangeIndicator = districtData.RangeIndicator.GetDisabled<PooledMonoBehaviour>().gameObject;
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
    }
     
     #endregion
     
    #region Church

    public sealed class ChurchState : DistrictState
    {
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
        public override Attack Attack { get; }

        public ChurchState(DistrictData districtData, TowerData churchData, Vector3 position, int key) : base(districtData, position, key, churchData)
        {
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
            foreach (Entity entity in spawnedDataEntities)   
            {
                float3 pos = entityManager.GetComponentData<LocalTransform>(entity).Position;
                OriginPosition = pos;
                foreach (IEffect createdEffect in districtData.CreatedEffects)
                {
                    createdEffect.Perform(this);
                }
            }
        }
        
        private void RevertCreateEffects()
        {
            foreach (IEffect createdEffect in districtData.CreatedEffects)
            {
                createdEffect.Revert(this);
            }
        }

        private void AttackEffectsAdded(List<IEffect> effects)
        {
            if (requireTargeting)
            {
                Attack.OnEffectsAdded -= AttackEffectsAdded;
                return;
            }
            
            if (!DistrictStateUtility.SearchEffects(effects)) return; 
            
            Attack.OnEffectsAdded -= AttackEffectsAdded;
            requireTargeting = true;
            
            RemoveEntities();
            SpawnEntities();
        }

        private void CreateStats()
        {
            stats = new Stats(districtData.Stats);
            
            UpgradeStat buffPower = new UpgradeStat(stats.Productivity, districtData.LevelDatas[0], 
                "Buff Power",
                new string[] { "Increase Buff Power by {0}", "Current Buff Power: {0}" },
                districtData.UpgradeIcons[0]){
                DistrictState = this
            };

            UpgradeStats.Add(buffPower);
        }
        
        public override void OnSelected(Vector3 pos)
        {
            if (selected) return;
            
            selected = true;
            rangeIndicator = districtData.RangeIndicator.GetDisabled<PooledMonoBehaviour>().gameObject;
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
    }

    #endregion

    #region Barracks

    public sealed class BarracksState : DistrictState
    {
        private GameObject rangeIndicator;
        
        public override List<IUpgradeStat> UpgradeStats { get; } = new List<IUpgradeStat>();
        public override Attack Attack { get; }
        
        public BarracksState(DistrictData districtData, TowerData data, Vector3 position, int key) : base(districtData, position, key, data)
        {
            CreateStats();
            SpawnEntitiesWithoutTargeting();
            
            Attack = new Attack(this.districtData.BaseAttack);
        }

        private void CreateStats()
        {
            stats = new Stats(districtData.Stats);
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
        }

        public override void OnDeselected()
        {
        }
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

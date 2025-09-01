using LocalToWorld = Unity.Transforms.LocalToWorld;
using Vector2 = UnityEngine.Vector2;
using System.Collections.Generic;
using Buildings.District.ECS;
using UnityEngine.Rendering;
using WaveFunctionCollapse;
using Math = Utility.Math;
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
using Utility;

namespace Buildings.District
{
    public abstract class DistrictState : IAttacker, IAttackerStatistics, IDisposable
    {
        public event Action OnStatisticsChanged;
        public event Action OnAttack;

        private readonly Dictionary<int2, List<Entity>> entityIndexes = new Dictionary<int2, List<Entity>>();
        protected readonly HashSet<int3> occupiedTargetMeshChunkIndex = new HashSet<int3>();
        protected readonly HashSet<Entity> spawnedDataEntities = new HashSet<Entity>();
        protected readonly HashSet<Entity> allSpawnedEntities = new HashSet<Entity>();
        
        protected readonly List<ChunkIndex> collapsedIndexes = new List<ChunkIndex>();
        
        protected DamageInstance lastDamageDone;
        protected EntityManager entityManager;
        protected TowerData districtData;
        protected Stats stats;

        private Entity simpleTargetingEntityPrefab;
        private Entity targetingEntityPrefab;
        private Entity targetEntityPrefab;
        
        protected bool requireTargeting;
        
        public CategoryType CategoryType => districtData.CategoryType;
        public DamageInstance LastDamageDone => lastDamageDone;
        public Stats Stats => stats;

        public abstract List<IUpgradeStat> UpgradeStats { get; }
        public PrototypeInfoData PrototypeInfo { get; set; }
        protected DistrictData DistrictData { get; }
        public Vector3 OriginPosition { get; set; }
        public Vector3 AttackPosition { get; set; }
        public abstract Attack Attack { get; }
        public float DamageDone { get; set; }
        public float GoldGained { get; set; }
        public int Level { get; set; } = 1;
        public bool IsDebug { get; set; }
        public int Key { get; set; }
        
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
            
            DistrictData.DistrictGenerator.OnDistrictChunkCleared += OnDistrictChunkCleared;
            DistrictData.DistrictGenerator.OnFinishedGenerating += OnFinishedGenerating;
            DistrictData.DistrictGenerator.OnCellCollapsed += OnCellCollapsed;
        }

        protected void SubscribeToStats()
        {
            stats.Range.OnValueChanged += RangeChanged;
            stats.AttackSpeed.OnValueChanged += AttackSpeedChanged;
        }

        private void OnFinishedGenerating()
        {
            if (collapsedIndexes.Count <= 0)
            {
                return;
            }
            
            SpawnEntities(collapsedIndexes);
            
            collapsedIndexes.Clear();
        }

        private void OnCellCollapsed(ChunkIndex chunkIndex)
        {
            if (!DistrictData.DistrictChunks.ContainsKey(chunkIndex.Index))
            {
                return;
            }

            int index = DistrictData.DistrictGenerator.ChunkWaveFunction[chunkIndex].PossiblePrototypes[0].MeshRot.MeshIndex;
            if (PrototypeInfo.PrototypeTargetIndexes.Contains(index))
            {
                collapsedIndexes.Add(chunkIndex);
            }
        }
        
        private void OnDistrictChunkCleared(QueryChunk chunk)
        {
            if (!DistrictData.DistrictChunks.ContainsKey(chunk.ChunkIndex))
            {
                return;
            }

            RemoveEntities(new HashSet<int3> { chunk.ChunkIndex });
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

            if (districtData.UseTargetMesh)
            {
                ComponentType[] targetComponents =
                {
                    typeof(ArchedMovementComponent),
                    typeof(TargetMeshComponent),
                    typeof(LocalTransform),
                    typeof(SpeedComponent),
                    typeof(LocalToWorld),
                    typeof(Prefab),
                };
                targetEntityPrefab = entityManager.CreateEntity(targetComponents);
                entityManager.SetComponentData(targetEntityPrefab, new SpeedComponent { Speed = 1});
                    
                RenderMeshDescription desc = new RenderMeshDescription(shadowCastingMode: ShadowCastingMode.On, receiveShadows: true);
                RenderMeshArray renderMeshArray = new RenderMeshArray(new Material[] { districtData.MeshVariable.Material }, new Mesh[] { districtData.MeshVariable.Mesh });
                RenderMeshUtility.AddComponents(targetEntityPrefab, entityManager, desc, renderMeshArray, MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));
            }
        }

        public virtual void Update(){}
        public abstract void OnSelected(Vector3 pos);
        public abstract void OnDeselected();
        public virtual void OnWaveStart() { }
        public virtual void OnWaveEnd() { }
        public virtual void OnUnitKill() { }
        
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
        
        protected void InvokeStatisticsChanged()
        {
            OnStatisticsChanged?.Invoke();
        }
        
        public virtual void Dispose()
        {
            DistrictData.DistrictGenerator.OnDistrictChunkCleared -= OnDistrictChunkCleared;
            DistrictData.DistrictGenerator.OnFinishedGenerating -= OnFinishedGenerating;
            DistrictData.DistrictGenerator.OnCellCollapsed -= OnCellCollapsed;

            DamageCallbackHandler.DamageDoneEvent.Remove(Key);
            
            entityManager.DestroyEntity(simpleTargetingEntityPrefab);
            entityManager.DestroyEntity(targetingEntityPrefab);
            entityManager.DestroyEntity(targetEntityPrefab);
            
            RemoveAllEntities();
        }

        #region Entities

        protected virtual List<ChunkIndex> GetEntityChunks(List<ChunkIndex> chunkIndexes, out List<Vector2> offsets, out List<Vector2> directions)
        {
            offsets = new Vector2[chunkIndexes.Count].ToList();
            directions = new List<Vector2>();
            for (int i = 0; i < chunkIndexes.Count; i++)
            {
                directions.Add(Vector2.up);
            }
            return chunkIndexes;
        }
        
        protected virtual void SpawnEntities(List<ChunkIndex> chunkIndexes)
        {
            List<ChunkIndex> topIndexes = GetEntityChunks(chunkIndexes, out List<Vector2> offsets, out List<Vector2> directions);
#if UNITY_EDITOR
            if (IsDebug)
            {
                Debug.Log($"Spawning {topIndexes.Count} entities, Total: {spawnedDataEntities.Count + topIndexes.Count}");
            }
#endif

            int count = topIndexes.Count;
            float attackSpeedValue = 1.0f / stats.AttackSpeed.Value;
            float delay = attackSpeedValue / count;
            
            NativeArray<Entity> entities = entityManager.Instantiate(requireTargeting ? targetingEntityPrefab : simpleTargetingEntityPrefab, count, Allocator.Temp);
            
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = GetPosition(topIndexes[i], offsets[i]);

                if (requireTargeting) SetupShooterEntity(entities[i], i, pos);
                else SetupSimpleShooterEntity(entities[i], i, pos);
                
                int2 key = topIndexes[i].Index.xz.MultiplyByAxis(DistrictStateUtility.Size) + topIndexes[i].CellIndex.xz;
                AddEntityToDictionary(key, entities[i], true);
            }
            
            if (districtData.UseTargetMesh)
            {
                SpawnTargetEntities(entities, topIndexes, offsets, directions);
            }
            
            entities.Dispose();

            void SetupShooterEntity(Entity spawnedEntity, int i, Vector3 pos)
            {
                entityManager.SetComponentData(spawnedEntity, new AttackSpeedComponent { AttackSpeed = attackSpeedValue, Timer = delay * i });
                entityManager.SetComponentData(spawnedEntity, new RangeComponent { Range = stats.Range.Value });
                entityManager.SetComponentData(spawnedEntity, new DistrictDataComponent { DistrictID = Key, });
                entityManager.SetComponentData(spawnedEntity, new LocalTransform { Position = pos });
                entityManager.SetComponentData(spawnedEntity, new DirectionRangeComponent
                {
                    Direction = directions[i],
                    Angle = districtData.AttackAngle,
                });
            }
            
            void SetupSimpleShooterEntity(Entity spawnedEntity, int i, Vector3 pos)
            {
                entityManager.SetComponentData(spawnedEntity, new AttackSpeedComponent { AttackSpeed = attackSpeedValue, Timer = delay * i });
                entityManager.SetComponentData(spawnedEntity, new DistrictDataComponent { DistrictID = Key, });
                entityManager.SetComponentData(spawnedEntity, new LocalTransform { Position = pos });
            }
        }
        
        private void SpawnTargetEntities(NativeArray<Entity> entities, List<ChunkIndex> topIndexes, List<Vector2> offsets, List<Vector2> directions)
        {
            NativeArray<Entity> targetEntities = entityManager.Instantiate(targetEntityPrefab, entities.Length, Allocator.Temp);

            for (int i = 0; i < targetEntities.Length; i++)
            {
                Vector3 pos = GetPosition(topIndexes[i], offsets[i]);

                SetupTargetEntity(targetEntities[i], entities[i], pos, directions[i]);

                int2 key = topIndexes[i].Index.xz.MultiplyByAxis(DistrictStateUtility.Size) + topIndexes[i].CellIndex.xz;
                AddEntityToDictionary(key, targetEntities[i], false);
            }
            

            void SetupTargetEntity(Entity spawnedEntity, Entity shooterEntity, Vector3 pos, Vector2 direction)
            {
                Vector3 upPosition = pos + new Vector3(0, 1.5f, 0);
                entityManager.SetComponentData(spawnedEntity, new TargetMeshComponent { Target = shooterEntity });
                entityManager.SetComponentData(spawnedEntity, new LocalTransform
                {
                    Position = upPosition,
                    Rotation = quaternion.LookRotation(direction.ToXyZ(0), Vector3.up),
                    Scale = districtData.MeshVariable.Scale
                });
                
                entityManager.SetComponentData(spawnedEntity, new ArchedMovementComponent
                {
                    EndPosition = pos,
                    StartPosition = upPosition,
                    Pivot = (pos + upPosition) / 2.0f
                });
            }
        }
        
        private Vector3 GetPosition(ChunkIndex chunkIndex, Vector2 offset)
        {
            QueryChunk chunk = DistrictData.DistrictChunks[chunkIndex.Index];
            Vector3 scaledOffset = DistrictData.DistrictGenerator.ChunkScale.MultiplyByAxis(offset.ToXyZ(0));
            Vector3 cellOffset = DistrictData.DistrictGenerator.CellSize.MultiplyByAxis(new Vector3(0.5f + chunkIndex.CellIndex.x, chunkIndex.CellIndex.y, 0.5f + chunkIndex.CellIndex.z));
            Vector3 pos = chunk.Position + scaledOffset + cellOffset;
            return pos;
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

        public void RemoveEntities(ChunkIndex chunkIndex)
        {
            int2 index = chunkIndex.Index.xz.MultiplyByAxis(DistrictStateUtility.Size) + chunkIndex.CellIndex.xz;
#if UNITY_EDITOR
            if (IsDebug)
            {
                Debug.Log($"Removing entities from index: {index}");
            }
#endif
            if (!entityIndexes.TryGetValue(index, out List<Entity> entities)) return;
                
            for (int i = 0; i < entities.Count; i++)
            {
                Entity entity = entities[i];
                
                DistrictStateUtility.RemoveChunkIndexFromOccupied(index, occupiedTargetMeshChunkIndex);
                spawnedDataEntities.Remove(entity);
                allSpawnedEntities.Remove(entity);
                entityManager.DestroyEntity(entity);
            }
            
            entityIndexes.Remove(index);
        }
        
        public void RemoveEntities(HashSet<int3> destroyedIndexes)
        {
            List<Entity> toRemove = new List<Entity>();
            foreach (int3 destroyedIndex in destroyedIndexes)
            {
                for (int x = 0; x < DistrictStateUtility.Width; x++)
                for (int y = 0; y < DistrictStateUtility.Height; y++)
                {
                    int2 longIndex = destroyedIndex.xz.MultiplyByAxis(DistrictStateUtility.Size) + new int2(x, y);

                    #if UNITY_EDITOR
                    if (IsDebug)
                    {
                        Debug.Log($"Removing entities from index: {longIndex}");
                    }
                    #endif
                    
                    if (!entityIndexes.TryGetValue(longIndex, out List<Entity> entities)) continue;
                
                    for (int i = 0; i < entities.Count; i++)
                    {
                        Entity entity = entities[i];
                        toRemove.Add(entity);
                        
                        spawnedDataEntities.Remove(entity);
                        allSpawnedEntities.Remove(entity);
                        DistrictStateUtility.RemoveChunkIndexFromOccupied(longIndex, occupiedTargetMeshChunkIndex);
                    }
                    
                    entityIndexes.Remove(longIndex);
                }
            }

            if (toRemove.Count <= 1)
            {
                for (int i = 0; i < toRemove.Count; i++)
                {
                    entityManager.DestroyEntity(toRemove[i]);
                }

                return;
            }
            
            NativeArray<Entity> remove = toRemove.ToNativeArray(Allocator.Temp);
            entityManager.DestroyEntity(remove);
            remove.Dispose();
        }
        
        protected virtual void RemoveAllEntities()
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

            SubscribeToStats();
        }

        protected override List<ChunkIndex> GetEntityChunks(List<ChunkIndex> chunkIndexes, out List<Vector2> outOffsets, out List<Vector2> outDirections)
        {
            List<ChunkIndex> indexes = new List<ChunkIndex>();
            List<Vector2> directions = new List<Vector2>();
            List<Vector2> offsets = new List<Vector2>();
            foreach (ChunkIndex chunkIndex in chunkIndexes)
            {
                ChunkIndex[] neighbours = ChunkWaveUtility.GetNeighbouringChunkIndexes(chunkIndex, DistrictStateUtility.Width,  DistrictStateUtility.Height);
                int rotation = DistrictData.DistrictGenerator.ChunkWaveFunction[chunkIndex].PossiblePrototypes[0].MeshRot.Rot;
                DistrictStateUtility.MeshTargetType targetType = DistrictStateUtility.GetMeshTargetType(neighbours, DistrictData.DistrictGenerator, PrototypeInfo, rotation);
                
                Vector2 forwardDir = Math.RotateVector2(new Vector2(0, 1), -90 * (rotation + 3) * math.TORADIANS);
                Vector2 leftDir = Math.RotateVector2(new Vector2(-1, 0), -90 * (rotation + 3) * math.TORADIANS);
                float2 baseKey = chunkIndex.Index.xz.MultiplyByAxis(DistrictStateUtility.Size) + chunkIndex.CellIndex.xz + new float2(0.5f, 0.5f);
                switch (targetType)
                {
                    case DistrictStateUtility.MeshTargetType.Corner:
                        Vector2 cornerRight = Math.RotateVector2(new Vector2(0.25f, 0f), -90 * (rotation + 3) * math.TORADIANS);
                        Vector2 cornerBack = Math.RotateVector2(new Vector2(0f, -0.25f), -90 * (rotation + 3) * math.TORADIANS);
                        float2 cornerRightKey = baseKey + Math.Rotate90Float2(new float2(0.5f, 0.5f), rotation + 3);
                        float2 cornerBackKey = baseKey + Math.Rotate90Float2(new float2(-0.5f, -0.5f), rotation + 3);
                        
                        AddPosition(cornerRight, forwardDir, chunkIndex, new int3((int)cornerRightKey.x, (int)cornerRightKey.y, rotation));
                        AddPosition(cornerBack, leftDir, chunkIndex, new int3((int)cornerBackKey.x, (int)cornerBackKey.y, (rotation + 3) % 4));
                        break;
                    case DistrictStateUtility.MeshTargetType.Side:
                        Vector2 sideRight = Math.RotateVector2(new Vector2(0.25f, 0f), -90 * (rotation + 3) * math.TORADIANS);
                        Vector2 sideLeft = Math.RotateVector2(new Vector2(-0.25f, 0f), -90 * (rotation + 3) * math.TORADIANS);
                        float2 sideRightKey = baseKey + Math.Rotate90Float2(new float2(0.5f, 0.5f), rotation + 3);
                        float2 sideLeftKey = baseKey + Math.Rotate90Float2(new float2(-0.5f, 0.5f), rotation + 3);
                        
                        AddPosition(sideRight, forwardDir, chunkIndex, new int3((int)sideRightKey.x, (int)sideRightKey.y, rotation));
                        AddPosition(sideLeft, forwardDir, chunkIndex, new int3((int)sideLeftKey.x, (int)sideLeftKey.y, rotation));
                        break;
                    default:
                        break;
                }
            } 
            
            outOffsets = offsets;
            outDirections = directions;
            return indexes;
            
            void AddPosition(Vector2 offset, Vector2 dir, ChunkIndex chunkIndex, int3 index)
            {
                if (occupiedTargetMeshChunkIndex.Contains(index))
                {
                    return;
                }
                
                Vector2 pivotOffset = -dir * 0.125f;
                offsets.Add(offset + pivotOffset);
                directions.Add(dir);
                indexes.Add(chunkIndex);
                
                occupiedTargetMeshChunkIndex.Add(index);
            }
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

        public override List<IUpgradeStat> UpgradeStats { get; } = new List<IUpgradeStat>();
        public override Attack Attack { get; }
        
        public BombState(DistrictData districtData, TowerData bombData, Vector3 position, int key) : base(districtData, position, key, bombData)
        {
            CreateStats();

            Attack = new Attack(this.districtData.BaseAttack);
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
            
            SubscribeToStats();
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
            
            SubscribeToStats();
        }
        
        private void OnWaveEnded()
        {
            
        }

        protected override List<ChunkIndex> GetEntityChunks(List<ChunkIndex> chunkIndexes, out List<Vector2> offsets, out List<Vector2> directions)
        {
            offsets = new List<Vector2> { Vector2.zero };
            directions = new List<Vector2> { Vector2.up };
            return new List<ChunkIndex> { chunkIndexes.First() };
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
            
            //RemoveEntities();
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
            
            SubscribeToStats();
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
 
            Attack = new Attack(flameData.BaseAttack);
        }
        
        /*protected override List<ChunkIndex> GetEntityChunks(List<ChunkIndex> chunkIndexes, out List<Vector2> offsets, out List<Vector2> directions)
        {
            List<ChunkIndex> indexes = new List<ChunkIndex>();
            HashSet<Tuple<int3, int2>> addedCorners = new HashSet<Tuple<int3, int2>>();
            offsets = new List<Vector2>();
            directions = new List<Vector2>();
 
            foreach (ChunkIndex index in chunkIndexes)
            {
                for (int corner = 0; corner < 4; corner++)
                {
                    int3 chunkIndex = index.Index;
                    int2 cornerDir = corners[corner]; 
                    if (addedCorners.Contains(Tuple.Create(chunkIndex, cornerDir)))
                    {
                        continue;
                    }
                    
                    bool isCornerValid = IsCornerValid(chunkIndex, cornerDir, PrototypeInfo, out Vector2 direction);
 
                    if (!isCornerValid) continue;
                    
                    indexes.Add(chunk);
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
        }*/
        
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
            
            SubscribeToStats();
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

            Attack = new Attack(flameData.BaseAttack);
        }
        
        /*
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
        */

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
            
            SubscribeToStats();
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

        /*protected override List<QueryChunk> GetEntityChunks(out List<Vector2> offsets, out List<Vector2> directions)
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
        }*/
        
        protected override void SpawnEntities(List<ChunkIndex> chunkIndexes)
        {
            base.SpawnEntities(chunkIndexes);

            PerformCreateEffects();
        }

        protected override void RemoveAllEntities()
        {
            base.RemoveAllEntities();

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
                if (createdEffect is IRevertableEffect revertableEffect)
                {
                    revertableEffect.Revert(this);
                }
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
            
            //RemoveEntities();
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
            
            SubscribeToStats();
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
            
            Attack = new Attack(this.districtData.BaseAttack);
        }

        private void CreateStats()
        {
            stats = new Stats(districtData.Stats);
            SubscribeToStats();
        }

        /*
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
        */
        
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
        public const int Width = 2;
        public const int Height = 2;
        public static int2 Size => new int2(Width, Height);
        
        public static bool SearchEffects(List<IEffect> effects)
        {
            foreach (IEffect effect in effects)
            {
                switch (effect)
                {
                    case IDamageEffect damageEffect:
                        return damageEffect.IsDamageEffect;
                    case IEffectHolder holder when SearchEffects(holder.Effects):
                        return true;
                }
            }

            return false;
        }
        
        // Order of neighbours:
        // Right, Forward, Left, Back 
        private static bool[] GetIfAdjacentSidesExist(ChunkIndex[] neighbours, IChunkWaveFunction<QueryChunk> waveFunction, PrototypeInfoData protInfo)
        {
            bool right = NeighbourExists(neighbours[0], waveFunction, protInfo, Direction.Left);
            bool forward = NeighbourExists(neighbours[1], waveFunction, protInfo, Direction.Backward);
            bool left = NeighbourExists(neighbours[2], waveFunction, protInfo, Direction.Right);
            bool back = NeighbourExists(neighbours[3], waveFunction, protInfo, Direction.Forward);
            return new bool[4] { right, forward, left, back };
        }

        private static bool NeighbourExists(ChunkIndex chunkIndex, IChunkWaveFunction<QueryChunk> waveFunction, PrototypeInfoData protInfo, Direction oppositeDirection)
        {
            if (!waveFunction.ChunkWaveFunction.Chunks.TryGetValue(chunkIndex.Index, out QueryChunk chunk))
            {
                return false;
            }

            if (chunk.PrototypeInfoData != protInfo)
            {
                return false;
            }

            if (chunk[chunkIndex.CellIndex].PossiblePrototypes[0].DirectionToKey(oppositeDirection) == 1)
            {
                return false;
            }

            return true;
        }

        public static MeshTargetType GetMeshTargetType(ChunkIndex[] neighbours, DistrictGenerator waveFunction, PrototypeInfoData protInfo, int rotation)
        {
            bool[] sides = GetIfAdjacentSidesExist(neighbours, waveFunction, protInfo);
            ListRotator.RotateInPlace(sides, rotation + 3);
            
            // Right, Forward, Left, Back
            return (sides[0], sides[1], sides[2], sides[3]) switch
            {
                (true, false, false, true) => MeshTargetType.Corner,
                (true, false, true, true) => MeshTargetType.Side,
                (true, true, true, true) => MeshTargetType.Full,
                _ => MeshTargetType.None
            };
        }

        public static void RemoveChunkIndexFromOccupied(int2 longIndex, HashSet<int3> occupiedChunks)
        {
            for (int x = 0; x < 2; x++)
            for (int y = 0; y < 2; y++)
            {
                int2 index = longIndex + new int2(x, y);
                for (int i = 0; i < 4; i++)
                {
                    occupiedChunks.Remove(new int3(index.x, index.y, i));
                }
            }
        }

        public enum MeshTargetType
        {
            None,
            Corner,
            Side,
            Full,
        }
    }
}

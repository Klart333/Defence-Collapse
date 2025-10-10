using Vector2 = UnityEngine.Vector2;
using Math = Utility.Math;

using System.Collections.Generic;
using Buildings.District.ECS;
using WaveFunctionCollapse;
using Gameplay.Upgrades;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Entities;
using DG.Tweening;
using Effects.ECS;
using UnityEngine;
using Enemy.ECS;
using Gameplay;
using Effects;
using Utility;
using System;
using Buildings.District.DistrictAttachment;

namespace Buildings.District
{
    public abstract class DistrictState : IAttacker, IAttackerStatistics, IDisposable
    {
        public event Action OnStatisticsChanged;
        public event Action OnAttack;

        private readonly Dictionary<int2, List<Entity>> entityIndexes = new Dictionary<int2, List<Entity>>();
        protected readonly HashSet<int3> occupiedTargetMeshChunkIndex = new HashSet<int3>();
        protected readonly HashSet<Entity> spawnedDataEntities = new HashSet<Entity>();
        private readonly HashSet<Entity> allSpawnedEntities = new HashSet<Entity>();
        private readonly List<ChunkIndex> collapsedIndexes = new List<ChunkIndex>();

        protected TowerData districtData;
        protected Stats stats;

        protected EntityManager entityManager;
        private Entity targetingEntityPrefab;
        private Entity targetEntityPrefab;
        
        public CategoryType CategoryType => districtData.CategoryType;
        public Stats Stats => stats;

        public DamageInstance LastDamageDone { get; private set; }
        public abstract List<IUpgradeStat> UpgradeStats { get; }
        public Vector3 OriginPosition { get; set; }
        public Vector3 AttackPosition { get; set; }
        public DistrictData DistrictData { get; }
        public abstract Attack Attack { get; }
        public float DamageDone { get; set; }
        public float GoldGained { get; set; }
        public int Level { get; set; } = 1;
        public int Key { get; set; }
        
        protected DistrictState(DistrictData districtData, Vector3 position, int key, TowerData towerData)
        {
            this.districtData = towerData;
            
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
        
        protected virtual void CreateStats()
        {
            stats = new Stats(districtData.Stats);
            foreach (IUpgradeStatEditor upgradeStatEditor in districtData.UpgradeStats)
            {
                IUpgradeStat upgradeStat = upgradeStatEditor.GetUpgradeStat(this);
                UpgradeStats.Add(upgradeStat);
            }
            
            SubscribeToStats();
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

            if (!districtData.UseMeshBasedPlacement)
            {
                collapsedIndexes.Add(chunkIndex);
                return;
            }
            
            int index = DistrictData.DistrictGenerator.ChunkWaveFunction[chunkIndex].PossiblePrototypes[0].MeshRot.MeshIndex;
            if (districtData.PrototypeInfoData.PrototypeTargetIndexes.Contains(index))
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

            if (!districtData.UseTargetMesh) return;
            
            Entity attachmentDatabase = entityManager.CreateEntityQuery(typeof(DistrictAttachmentDatabaseTag)).GetSingletonEntity();
            targetEntityPrefab = entityManager.GetBuffer<DistrictAttachmentElement>(attachmentDatabase)[districtData.DistrictAttachmentIndex].DistrictAttachment;
        }

        public abstract void OnSelected(Vector3 pos);
        public abstract void OnDeselected();
        public virtual void OnUnitKill() { }
        
        private void OnDamageDone(DamageCallbackComponent damageCallback)
        {
            DamageDone += damageCallback.DamageTaken;
            OnStatisticsChanged?.Invoke();
            if (!damageCallback.TriggerDamageDone) return;
            
            LastDamageDone = new DamageInstance
            {
                Damage = damageCallback.DamageTaken,
                AttackPosition = damageCallback.Position,
                Source = this,
            };
            
            Attack?.OnDoneDamage(this);
        }

        public void OnUnitDoneDamage(DamageInstance damageInstance)
        {
            LastDamageDone = damageInstance;

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

            if (GameManager.Instance.IsGameOver || !World.DefaultGameObjectInjectionWorld.IsCreated)
            {
                return;
            }

            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            entityManager.DestroyEntity(targetingEntityPrefab);
            entityManager.DestroyEntity(targetEntityPrefab);
            
            RemoveAllEntities();
        }

        #region Entities

        protected virtual List<TargetEntityIndex> GetEntityChunks(List<ChunkIndex> chunkIndexes)
        {
            List<TargetEntityIndex> indexes = new List<TargetEntityIndex>();
            HashSet<int3> uniqueChunks = new HashSet<int3>();
            for (int i = 0; i < chunkIndexes.Count; i++)
            {
                if (uniqueChunks.Add(chunkIndexes[i].Index))
                {
                    indexes.Add(new TargetEntityIndex
                    {
                        ChunkIndex = chunkIndexes[i],
                        Direction = Vector2.up,
                    });
                }
            }
            return indexes;
        }
        
        protected virtual void SpawnEntities(List<ChunkIndex> chunkIndexes)
        {
            List<TargetEntityIndex> targetIndexes = GetEntityChunks(chunkIndexes);
#if UNITY_EDITOR
            if (DistrictData.DistrictHandler.IsDebug)
            {
                Debug.Log($"Spawning {targetIndexes.Count} entities, Total: {spawnedDataEntities.Count + targetIndexes.Count}");
            }
#endif
            int count = targetIndexes.Count;
            float attackSpeedValue = 1.0f / stats.AttackSpeed.Value;
            
            NativeArray<Entity> entities = entityManager.Instantiate(targetingEntityPrefab, count, Allocator.Temp);
            
            for (int i = 0; i < count; i++)
            {
                TargetEntityIndex index = targetIndexes[i];
                Vector3 pos = GetPosition(index.ChunkIndex, index.Offset);

                SetupShooterEntity(entities[i], i, pos);
                
                int2 key = index.ChunkIndex.Index.xz.MultiplyByAxis(DistrictStateUtility.Size) + index.ChunkIndex.CellIndex.xz;
                AddEntityToDictionary(key, entities[i], true);
            }
            
            if (districtData.UseTargetMesh)
            {
                SpawnTargetEntities(entities, targetIndexes);
            }
            
            entities.Dispose();

            void SetupShooterEntity(Entity spawnedEntity, int i, Vector3 pos)
            {
                entityManager.SetComponentData(spawnedEntity, new AttackSpeedComponent { AttackSpeed = attackSpeedValue, AttackTimer = attackSpeedValue});
                entityManager.SetComponentData(spawnedEntity, new RangeComponent { Range = stats.Range.Value });
                entityManager.SetComponentData(spawnedEntity, new DistrictDataComponent { DistrictID = Key, });
                entityManager.SetComponentData(spawnedEntity, new LocalTransform { Position = pos });
                entityManager.SetComponentData(spawnedEntity, new DirectionRangeComponent
                {
                    Direction = math.normalize(targetIndexes[i].Direction),
                    Angle = districtData.AttackAngle,
                });
            }
        }
        
        private void SpawnTargetEntities(NativeArray<Entity> entities, List<TargetEntityIndex> targetIndexes)
        {
            NativeArray<Entity> targetEntities = entityManager.Instantiate(targetEntityPrefab, entities.Length, Allocator.Temp);

            for (int i = 0; i < targetEntities.Length; i++)
            {
                TargetEntityIndex index = targetIndexes[i];
                Vector3 pos = GetPosition(index.ChunkIndex, index.Offset);
                
                SetupTargetEntity(targetEntities[i], entities[i], pos, index.Direction);

                int2 key = index.ChunkIndex.Index.xz.MultiplyByAxis(DistrictStateUtility.Size) + index.ChunkIndex.CellIndex.xz;
                AddEntityToDictionary(key, targetEntities[i], false);
            }
            

            void SetupTargetEntity(Entity spawnedEntity, Entity shooterEntity, Vector3 pos, Vector2 direction)
            {
                Vector3 upPosition = pos + new Vector3(0, 1.5f, 0);
                entityManager.SetComponentData(spawnedEntity, new AttachementMeshComponent
                {
                    Target = shooterEntity,
                    AttachmentMeshEntity = entityManager.GetComponentData<AttachementMeshComponent>(spawnedEntity).AttachmentMeshEntity,
                });
                entityManager.SetComponentData(spawnedEntity, new LocalTransform
                {
                    Position = upPosition,
                    Rotation = quaternion.LookRotation(direction.ToXyZ(0), Vector3.up),
                    Scale = entityManager.GetComponentData<LocalTransform>(spawnedEntity).Scale,
                });
                
                entityManager.SetComponentData(spawnedEntity, new SmoothMovementComponent
                {
                    EndPosition = pos,
                    StartPosition = upPosition,
                    Ease = Ease.Linear,
                });
            }
        }
        
        private Vector3 GetPosition(ChunkIndex chunkIndex, Vector3 offset)
        {
            QueryChunk chunk = DistrictData.DistrictChunks[chunkIndex.Index];
            Vector3 scaledOffset = DistrictData.DistrictGenerator.ChunkScale.MultiplyByAxis(offset);
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
            foreach (Entity entity in spawnedDataEntities)
            {
                entityManager.SetComponentData(entity, new AttackSpeedComponent
                {
                    AttackSpeed = attackSpeedValue, 
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
                    if (DistrictData.DistrictHandler.IsDebug)
                    {
                        //Debug.Log($"Removing entities from index: {longIndex}");
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

        public DistrictAttachmentData[] GetAttachmentDatas()
        {
            DistrictAttachmentData[] attachmentDatas = new DistrictAttachmentData[spawnedDataEntities.Count];
            int index = 0;
            foreach (Entity dataEntity in spawnedDataEntities)
            {
                AttackSpeedComponent attackSpeed = entityManager.GetComponentData<AttackSpeedComponent>(dataEntity);
                EnemyTargetComponent enemyTarget = entityManager.GetComponentData<EnemyTargetComponent>(dataEntity);
                LocalTransform transform = entityManager.GetComponentData<LocalTransform>(dataEntity);
                
                attachmentDatas[index++] = new DistrictAttachmentData
                {
                    TargetPosition = enemyTarget.TargetPosition,
                    AttackSpeed = attackSpeed.AttackSpeed,
                    AttackTimer = attackSpeed.AttackTimer,
                    HasTarget = enemyTarget.HasTarget,
                    Position = transform.Position,
                };
            }
            
            return attachmentDatas;
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

        protected override List<TargetEntityIndex> GetEntityChunks(List<ChunkIndex> chunkIndexes)
        {
            return DistrictStateUtility.GetFourCornersEntityIndexes(chunkIndexes, occupiedTargetMeshChunkIndex, DistrictData.DistrictGenerator, districtData.PrototypeInfoData, 2);
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

    [System.Serializable]
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
        
        protected override List<TargetEntityIndex> GetEntityChunks(List<ChunkIndex> chunkIndexes)
        {
            return DistrictStateUtility.GetFourCornersEntityIndexes(chunkIndexes, occupiedTargetMeshChunkIndex, DistrictData.DistrictGenerator, districtData.PrototypeInfoData, 2);
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

    public sealed class TownHallState : DistrictState, ITurnCompleteSubscriber
    {
        private GameObject rangeIndicator;
        private bool selected;

        public override List<IUpgradeStat> UpgradeStats { get; } = new List<IUpgradeStat>();
        public override Attack Attack { get; }
        
        public TownHallState(DistrictData districtData, TowerData townhallData, Vector3 position, int key) : base(districtData, position, key, townhallData)
        {
            CreateStats();
            
            Attack = new Attack(townhallData.BaseAttack);
        }
        
        protected override void RangeChanged()
        {
            base.RangeChanged();
            
            if (selected && rangeIndicator is not null && rangeIndicator.activeSelf)
            {
                rangeIndicator.transform.localScale = new Vector3(stats.Range.Value * 2.0f, 0.01f, stats.Range.Value * 2.0f);
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
        
        public void TurnComplete()
        {
            HashSet<int> heights = new HashSet<int>();
            foreach (KeyValuePair<int3, QueryChunk> chunkIndex in DistrictData.DistrictChunks)
            {
                if (!heights.Add(chunkIndex.Key.y)) continue;
                
                foreach (IEffect effect in districtData.EndWaveEffects)
                {
                    effect.Perform(this);
                }
            }
                
            InvokeStatisticsChanged();
        }
    }

    #endregion
    
    #region Mine

    public sealed class MineState : DistrictState, ITurnCompleteSubscriber
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
        }

        protected override void RangeChanged()
        {
            base.RangeChanged();

            if (selected && rangeIndicator is not null && rangeIndicator.activeSelf)
            {
                rangeIndicator.transform.localScale = new Vector3(stats.Range.Value * 2.0f, 0.01f, stats.Range.Value * 2.0f);
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
            if (rangeIndicator is null) return;
            
            selected = false;
            rangeIndicator.SetActive(false);
            rangeIndicator = null;
        }

        public void TurnComplete()
        {
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

        public override List<IUpgradeStat> UpgradeStats { get; } = new List<IUpgradeStat>();
        public override Attack Attack { get; }
        
        public FlameState(DistrictData districtData, TowerData flameData, Vector3 position, int key) : base(districtData, position, key, flameData)
        {
            CreateStats();
 
            Attack = new Attack(flameData.BaseAttack);
        }
        
        protected override List<TargetEntityIndex> GetEntityChunks(List<ChunkIndex> chunkIndexes)
        {
            return DistrictStateUtility.GetPerimeterEntityChunks(chunkIndexes, occupiedTargetMeshChunkIndex, DistrictData.DistrictGenerator, districtData.PrototypeInfoData, 2);
        }

        protected override void RangeChanged()
        {
            base.RangeChanged();

            if (selected && rangeIndicator is not null && rangeIndicator.activeSelf)
            {
                rangeIndicator.transform.localScale = new Vector3(stats.Range.Value * 2.0f, 0.01f, stats.Range.Value * 2.0f);
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
        
        protected override List<TargetEntityIndex> GetEntityChunks(List<ChunkIndex> chunkIndexes)
        {
            return DistrictStateUtility.GetFullChunkEntityIndexes(chunkIndexes, DistrictData.DistrictGenerator.ChunkScale);
        }

        protected override void RangeChanged()
        {
            base.RangeChanged();

            if (selected && rangeIndicator is not null && rangeIndicator.activeSelf)
            {
                rangeIndicator.transform.localScale = new Vector3(stats.Range.Value * 2.0f, 0.01f, stats.Range.Value * 2.0f);
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
     
    #region Church

    public sealed class ChurchState : DistrictState
    {
        private GameObject rangeIndicator;

        private bool selected;
        
        public override List<IUpgradeStat> UpgradeStats { get; } = new List<IUpgradeStat>();
        public override Attack Attack { get; }

        public ChurchState(DistrictData districtData, TowerData churchData, Vector3 position, int key) : base(districtData, position, key, churchData)
        {
            CreateStats(); 
            Attack = new Attack(churchData.BaseAttack);
            
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

        protected override List<TargetEntityIndex> GetEntityChunks(List<ChunkIndex> chunkIndexes)
        {
            return DistrictStateUtility.GetFullChunkEntityIndexes(chunkIndexes, DistrictData.DistrictGenerator.ChunkScale, Vector3.up);
        }

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
        
        protected override List<TargetEntityIndex> GetEntityChunks(List<ChunkIndex> chunkIndexes)
        {
            return DistrictStateUtility.GetFullChunkEntityIndexes(chunkIndexes, DistrictData.DistrictGenerator.ChunkScale);
        }
        
        public override void OnSelected(Vector3 pos)
        {
        }

        public override void OnDeselected()
        {
        }
    }

    #endregion
    
    #region Mine

    public sealed class LumbermillState : DistrictState, ITurnCompleteSubscriber, ILumbermillStatistics
    { 
        private GameObject rangeIndicator;
        private bool selected;
        public override List<IUpgradeStat> UpgradeStats { get; } = new List<IUpgradeStat>();
        public int TurnsUntilComplete { get; set; }
        public override Attack Attack { get; }
        
        public LumbermillState(DistrictData districtData, TowerData lumbermillData, Vector3 position, int key) : base(districtData, position, key, lumbermillData)
        {
            this.districtData = lumbermillData;
            TurnsUntilComplete = 5;
            
            CreateStats();
            Attack = new Attack(lumbermillData.BaseAttack);
        }
        
        protected override void RangeChanged()
        {
            base.RangeChanged();
            if (selected && rangeIndicator is not null && rangeIndicator.activeSelf)
            {
                rangeIndicator.transform.localScale = new Vector3(stats.Range.Value * 2.0f, 0.01f, stats.Range.Value * 2.0f);
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
            if (rangeIndicator is null) return;
            
            selected = false;
            rangeIndicator.SetActive(false);
            rangeIndicator = null;
        }
        public void TurnComplete()
        {
            TurnsUntilComplete--;
            foreach (QueryChunk chunk in DistrictData.DistrictChunks.Values)
            {
                OriginPosition = chunk.Position;
                foreach (IEffect effect in districtData.EndWaveEffects)
                {
                    effect.Perform(this);
                }
            }
            
            InvokeStatisticsChanged(); 
        }
    }

    #endregion

    public static class DistrictStateUtility
    {
        private enum MeshTargetType
        {
            None,
            Corner,
            Side,
            Full,
            InCorner,
        }

        public const int Width = 2;
        public const int Height = 2;
        public static int2 Size => new int2(Width, Height);
        
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

        private static MeshTargetType GetMeshTargetType(ChunkIndex chunkIndex, ChunkIndex[] neighbours, DistrictGenerator waveFunction, PrototypeInfoData protInfo, int rotation)
        {
            rotation %= 4;
            bool[] sides = GetIfAdjacentSidesExist(neighbours, waveFunction, protInfo);
            ListRotator.RotateInPlace(sides, rotation);
            
            // Right, Forward, Left, Back
            return (sides[0], sides[1], sides[2], sides[3]) switch
            {
                (true, false, false, true) => MeshTargetType.Corner,
                (true, false, true, true) => MeshTargetType.Side,
                (true, true, true, true) => PrototypeDataUtility.IsKeySymmetrical(rotation switch
                   {
                       0 or 1 => waveFunction.ChunkWaveFunction[chunkIndex].PossiblePrototypes[0].PosZ,
                       2 or 3 => waveFunction.ChunkWaveFunction[chunkIndex].PossiblePrototypes[0].NegZ,
                       _ => throw new ArgumentOutOfRangeException(rotation.ToString()),
                   } ) ? MeshTargetType.Full : MeshTargetType.InCorner,
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
        
        public static List<TargetEntityIndex> GetPerimeterEntityChunks(List<ChunkIndex> chunkIndexes, HashSet<int3> occupiedTargetMeshChunkIndex, DistrictGenerator districtGenerator, PrototypeInfoData protInfo, int rotationOffset)
        {
            List<TargetEntityIndex> entityIndexes = new List<TargetEntityIndex>();
            foreach (ChunkIndex chunkIndex in chunkIndexes)
            {
                ChunkIndex[] neighbours = ChunkWaveUtility.GetNeighbouringChunkIndexes(chunkIndex, Width,  Height);
                int rotation = districtGenerator.ChunkWaveFunction[chunkIndex].PossiblePrototypes[0].MeshRot.Rot + rotationOffset;
                MeshTargetType targetType = GetMeshTargetType(chunkIndex, neighbours, districtGenerator, protInfo, rotation);
                
                Vector2 forwardDir = Math.RotateVector2(new Vector2(0, 1), -90 * rotation * math.TORADIANS);
                Vector2 leftDir = Math.RotateVector2(new Vector2(-1, 0), -90 * rotation * math.TORADIANS);
                float2 baseKey = chunkIndex.Index.xz.MultiplyByAxis(Size) + chunkIndex.CellIndex.xz + new float2(0.5f, 0.5f);
                switch (targetType)
                {
                    case MeshTargetType.Corner:
                        Vector2 cornerRight = Math.RotateVector2(new Vector2(0.25f, 0f), -90 * rotation * math.TORADIANS);
                        Vector2 cornerBack = Math.RotateVector2(new Vector2(0f, -0.25f), -90 * rotation * math.TORADIANS);
                        float2 cornerRightKey = baseKey + Math.Rotate90Float2(new float2(0.5f, 0.5f), rotation);
                        float2 cornerBackKey = baseKey + Math.Rotate90Float2(new float2(-0.5f, -0.5f), rotation);
                        
                        AddPosition(cornerRight, forwardDir, chunkIndex, new int3((int)cornerRightKey.x, (int)cornerRightKey.y, (rotation - rotationOffset) % 4));
                        AddPosition(cornerBack, leftDir, chunkIndex, new int3((int)cornerBackKey.x, (int)cornerBackKey.y, (rotation - rotationOffset + 3) % 4));
                        break;
                    case MeshTargetType.Side:
                        Vector2 sideRight = Math.RotateVector2(new Vector2(0.25f, 0f), -90 * rotation * math.TORADIANS);
                        Vector2 sideLeft = Math.RotateVector2(new Vector2(-0.25f, 0f), -90 * rotation * math.TORADIANS);
                        float2 sideRightKey = baseKey + Math.Rotate90Float2(new float2(0.5f, 0.5f), rotation);
                        float2 sideLeftKey = baseKey + Math.Rotate90Float2(new float2(-0.5f, 0.5f), rotation);
                        
                        AddPosition(sideRight, forwardDir, chunkIndex, new int3((int)sideRightKey.x, (int)sideRightKey.y, (rotation - rotationOffset) % 4));
                        AddPosition(sideLeft, forwardDir, chunkIndex, new int3((int)sideLeftKey.x, (int)sideLeftKey.y, (rotation - rotationOffset) % 4));
                        break;
                }
            } 
            
            return entityIndexes;
            
            void AddPosition(Vector2 offset, Vector2 dir, ChunkIndex chunkIndex, int3 index)
            {
                if (occupiedTargetMeshChunkIndex.Contains(index))
                {
                    return;
                }
                
                Vector2 pivotOffset = -dir * 0.0625f;
                entityIndexes.Add(new TargetEntityIndex
                {
                    ChunkIndex = chunkIndex,
                    Offset = (offset + pivotOffset).ToXyZ(0),
                    Direction = dir
                });
                
                occupiedTargetMeshChunkIndex.Add(index);
            }
        }
        
        public static List<TargetEntityIndex> GetFullChunkEntityIndexes(List<ChunkIndex> chunkIndexes, Vector3 chunkScale, Vector3 offset = default)
        {
            Dictionary<int3, List<ChunkIndex>> chunkIndexLookup = new Dictionary<int3, List<ChunkIndex>>();
            List<TargetEntityIndex> entityIndexes = new List<TargetEntityIndex>();
            foreach (ChunkIndex chunkIndex in chunkIndexes)
            {
                if (chunkIndexLookup.TryGetValue(chunkIndex.Index, out List<ChunkIndex> list))
                {
                    if (list.Count < 3)
                    {
                        list.Add(chunkIndex);
                        continue;
                    }

                    chunkIndexLookup.Remove(chunkIndex.Index);
                    entityIndexes.Add(new TargetEntityIndex
                    {
                        ChunkIndex = new ChunkIndex(chunkIndex.Index, new int3()),
                        Offset = offset + chunkScale.XyZ(0) / 2.0f,
                        Direction = Vector2.up
                    });
                }
                else
                {
                    chunkIndexLookup.Add(chunkIndex.Index, new List<ChunkIndex>(4) { chunkIndex });
                }
            }
            
            return entityIndexes;
        }

        public static List<TargetEntityIndex> GetFourCornersEntityIndexes(List<ChunkIndex> chunkIndexes, HashSet<int3> occupiedTargetMeshChunkIndex, DistrictGenerator districtGenerator,
            PrototypeInfoData protInfo, int rotationOffset)
        {
            List<TargetEntityIndex> entityIndexes = new List<TargetEntityIndex>();
            foreach (ChunkIndex chunkIndex in chunkIndexes)
            {
                ChunkIndex[] neighbours = ChunkWaveUtility.GetNeighbouringChunkIndexes(chunkIndex, Width,  Height);
                int rotation = districtGenerator.ChunkWaveFunction[chunkIndex].PossiblePrototypes[0].MeshRot.Rot + rotationOffset;
                MeshTargetType targetType = GetMeshTargetType(chunkIndex, neighbours, districtGenerator, protInfo, rotation);
                float2 baseKey = chunkIndex.Index.xz.MultiplyByAxis(Size) + chunkIndex.CellIndex.xz + new float2(0.5f, 0.5f);

                // Down right, it's always valid 
                {
                    Vector2 downRight = Math.RotateVector2(new Vector2(0.25f, -0.25f), -90 * rotation * math.TORADIANS);
                    float2 downRightKey = baseKey + Math.Rotate90Float2(new float2(0.5f, -0.5f), rotation);
                        
                    AddPosition(downRight, chunkIndex, new int3((int)downRightKey.x, (int)downRightKey.y, 0));
                }

                // Down Left
                if (targetType is MeshTargetType.Side or MeshTargetType.InCorner or MeshTargetType.Full)
                {
                    Vector2 downLeft = Math.RotateVector2(new Vector2(-0.25f, -0.25f), -90 * rotation * math.TORADIANS);
                    float2 downLeftKey = baseKey + Math.Rotate90Float2(new float2(-0.5f, -0.5f), rotation);
                    AddPosition(downLeft, chunkIndex, new int3((int)downLeftKey.x, (int)downLeftKey.y, 0));
                }
                
                // Top Right
                if (targetType is MeshTargetType.InCorner or MeshTargetType.Full)
                {
                    Vector2 topRight = Math.RotateVector2(new Vector2(0.25f, 0.25f), -90 * rotation * math.TORADIANS);
                    float2 topRightKey = baseKey + Math.Rotate90Float2(new float2(0.5f, 0.5f), rotation);
                    AddPosition(topRight, chunkIndex, new int3((int)topRightKey.x, (int)topRightKey.y, 0));
                }
                
                // Top Left
                if (targetType is MeshTargetType.Full)
                {
                    Vector2 topLeft = Math.RotateVector2(new Vector2(-0.25f, 0.25f), -90 * rotation * math.TORADIANS);
                    float2 topLeftKey = baseKey + Math.Rotate90Float2(new float2(-0.5f, 0.5f), rotation);
                    AddPosition(topLeft, chunkIndex, new int3((int)topLeftKey.x, (int)topLeftKey.y, 0));
                }
            } 
            
            return entityIndexes;
            
            void AddPosition(Vector2 offset, ChunkIndex chunkIndex, int3 index)
            {
                if (occupiedTargetMeshChunkIndex.Contains(index))
                {
                    return;
                }
                
                entityIndexes.Add(new TargetEntityIndex
                {
                    ChunkIndex = chunkIndex,
                    Offset = offset.ToXyZ(0),
                    Direction = new Vector2(0, 1)
                });
                
                occupiedTargetMeshChunkIndex.Add(index); 
            }
        }
    }

    public struct TargetEntityIndex
    {
        public ChunkIndex ChunkIndex;
        public Vector3 Offset;
        public Vector2 Direction;
    }

    public interface ITurnCompleteSubscriber
    {
        public void TurnComplete();
    }
}

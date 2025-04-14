using DataStructures.Queue.ECS;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Entities;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Effects.ECS;
using WaveFunctionCollapse;

namespace Buildings.District
{
    [System.Serializable]
    public abstract class DistrictState : IAttacker
    {
        public event Action OnAttack;
        
        public float Range { get; set; }

        protected readonly List<Entity> spawnedEntities = new List<Entity>();
        
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
        public int Key { get; set; }

        protected DistrictState(DistrictData districtData, Vector3 position, int key)
        {
            this.districtData = districtData;
            OriginPosition = position;

            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            Key = key;
            
            CollisionSystem.DamageDoneEvent.Add(key, OnDamageDone);
        }

        public abstract void OnStateEntered();
        public abstract void Update();
        public abstract void OnSelected(Vector3 pos);
        public abstract void OnDeselected();
        public abstract void Die();
        public abstract void OnWaveStart(int cellCount);
        
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

    #region Archer

    public class ArcherState : DistrictState
    {
        private readonly Attack attack;
        private readonly TowerData archerData;
        
        private GameObject rangeIndicator;
        private bool selected;
        
        public override Attack Attack => attack;

        public ArcherState(DistrictData districtData, TowerData archerData, IEnumerable<Chunk> chunks, Vector3 position, int key) : base(districtData, position, key)
        {
            this.archerData = archerData;
            Range = archerData.Range;

            attack = new Attack(archerData.BaseAttack);
            stats = new Stats(archerData.Stats);

            SpawnEntities(chunks);
        }

        private void SpawnEntities(IEnumerable<Chunk> chunks)
        {
            List<Chunk> perimeterChunks = DistrictUtility.GetTopPerimeter(chunks);
            
            foreach (Chunk chunk in perimeterChunks)
            {
                ComponentType[] componentTypes =
                {
                    typeof(LocalTransform),
                    typeof(RangeComponent),
                    typeof(EnemyTargetComponent),
                    typeof(AttackSpeedComponent),
                };
            
                Entity spawnedEntity = entityManager.CreateEntity(componentTypes);
                entityManager.SetComponentData(spawnedEntity, new LocalTransform { Position = chunk.Position });
                entityManager.SetComponentData(spawnedEntity, new RangeComponent { Range = Range }); // These could be made to blob assets, probably easier to change then
                entityManager.SetComponentData(spawnedEntity, new AttackSpeedComponent
                {
                    AttackSpeed = 1.0f / archerData.Stats.AttackSpeed.Value, 
                });
                spawnedEntities.Add(spawnedEntity);
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

        public override void OnWaveStart(int cellCount)
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
        private readonly TowerData bombData;
        private readonly Attack attack;
        
        private GameObject rangeIndicator;
        private bool selected;
        
        public override Attack Attack => attack;
        
        public BombState(DistrictData districtData, TowerData bombData, IEnumerable<Chunk> chunks, Vector3 position, int key) : base(districtData, position, key)
        {
            this.bombData = bombData;
            Range = bombData.Range;

            SpawnEntities(chunks);
        }

        private void SpawnEntities(IEnumerable<Chunk> chunks)
        {
            List<Chunk> perimeterChunks = DistrictUtility.GetTopPerimeter(chunks); // Should probably reverse, so only inside cells shoot

            int index = 0;
            foreach (Chunk chunk in perimeterChunks)
            {
                ComponentType[] componentTypes =
                {
                    typeof(LocalTransform),
                    typeof(RangeComponent),
                    typeof(EnemyTargetComponent),
                    typeof(AttackSpeedComponent),
                };
            
                Entity spawnedEntity = entityManager.CreateEntity(componentTypes);
                entityManager.SetComponentData(spawnedEntity, new LocalTransform { Position = chunk.Position });
                // These could be made to blob assets, probably easier to change then
                entityManager.SetComponentData(spawnedEntity, new RangeComponent { Range = Range });
                entityManager.SetComponentData(spawnedEntity, new AttackSpeedComponent { AttackSpeed = 1.0f / bombData.Stats.AttackSpeed.Value, Timer = 0.1f * index++});
                spawnedEntities.Add(spawnedEntity);
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

        public override void OnWaveStart(int cellCount)
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
        private readonly Attack attack;
        private readonly TowerData mineData;

        private float mineCooldown = 0;        

        public override Attack Attack => attack;
        
        public MineState(DistrictData districtData, TowerData mineData, IEnumerable<Chunk> chunks, Vector3 position, int key) : base(districtData, position, key)
        {
            this.mineData = mineData;
            attack = mineData.BaseAttack;
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
            if (mineCooldown <= 0)
            {
                
            }
            else
            {
                mineCooldown -= Time.deltaTime;
            }
        }

        private void PerformAttack(float3 targetPosition)
        {
            
        }

        public override void OnWaveStart(int cellCount)
        {
            
        }

        public override void Die()
        {

        }
    }

    #endregion

}

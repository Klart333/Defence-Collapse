using DataStructures.Queue.ECS;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Entities;
using UnityEngine;
using System;
using Effects.ECS;

namespace Buildings.District
{
    [System.Serializable]
    public abstract class DistrictState : IAttacker
    {
        public event Action OnAttack;
        
        public float Range { get; set; }

        protected DamageInstance lastDamageDone;
        protected EntityManager entityManager;
        protected DistrictData districtData;
        protected Entity spawnedEntity;
        protected Stats stats;

        private float totalDamageDealt;
        
        public virtual Attack Attack { get; }
        public DamageInstance LastDamageDone => lastDamageDone;
        public Stats Stats => stats;
        public Vector3 OriginPosition { get; protected set; }
        public Vector3 AttackPosition { get; set; }
        public abstract LayerMask CollideWith { get; }
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
        public abstract void OnWaveStart(int houseCount);
        
        private void OnDamageDone(Entity entity)
        {
            DamageComponent damage = entityManager.GetComponentData<DamageComponent>(entity);
            totalDamageDealt += damage.Damage;
            Debug.Log($"Dealt {damage.Damage}, total: {totalDamageDealt}");
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

        private float attackCooldownTimer = 0;

        public ArcherState(DistrictData districtData, TowerData archerData, Vector3 position, int key) : base(districtData, position, key)
        {
            this.archerData = archerData;
            Range = archerData.Range;

            attack = new Attack(archerData.BaseAttack);
            stats = new Stats(archerData.Stats);

            ComponentType[] componentTypes = new ComponentType[3]
            {
                typeof(LocalTransform),
                typeof(RangeComponent),
                typeof(EnemyTargetComponent),
            };
            
            spawnedEntity = entityManager.CreateEntity(componentTypes);
            entityManager.SetComponentData(spawnedEntity, new LocalTransform { Position = position });
            entityManager.SetComponentData(spawnedEntity, new RangeComponent { Range = Range });
        }

        public override Attack Attack => attack;

        public override LayerMask CollideWith => archerData.AttackLayerMask;

        public override void OnStateEntered()
        {

        }

        public override void OnSelected(Vector3 pos)
        {
            rangeIndicator = archerData.RangeIndicator.GetAtPosAndRot<PooledMonoBehaviour>(pos, Quaternion.identity).gameObject;
            rangeIndicator.transform.localScale = new Vector3(Range * 2.0f, 0.01f, Range * 2.0f);
        }

        public override void OnDeselected()
        {
            if (rangeIndicator != null)
            {
                rangeIndicator.SetActive(false);
            }
        }

        public override void Update()
        {
            if (attackCooldownTimer <= 0)
            {
                Entity targetEntity = entityManager.GetComponentData<EnemyTargetComponent>(spawnedEntity).Target;
                if (entityManager.Exists(targetEntity))
                {
                    PerformAttack(entityManager.GetComponentData<LocalTransform>(targetEntity).Position);
                }
                else
                {
                    attackCooldownTimer = 0.1f;
                }
            }
            else
            {
                attackCooldownTimer -= Time.deltaTime;
            }
        }

        private void PerformAttack(float3 targetPosition)
        {
            AttackPosition = targetPosition;
            attack.TriggerAttack(this);
            attackCooldownTimer = 1.0f / stats.AttackSpeed.Value;
        }

        public override void OnWaveStart(int houseCount)
        {
            MoneyManager.Instance.AddMoney(houseCount * archerData.IncomePerHouse);
        }

        public override void Die()
        {

        }
    }

    #endregion

    #region Bomb

    public class BombState : DistrictState
    {
        private readonly Attack attack;
        private readonly TowerData bombData;
        private GameObject rangeIndicator;

        private float attackCooldownTimer = 0;

        public BombState(DistrictData districtData, TowerData bombData, Vector3 position, int key) : base(districtData, position, key)
        {
            this.bombData = bombData;
            Range = bombData.Range;

            ComponentType[] componentTypes = new ComponentType[3]
            {
                typeof(LocalTransform),
                typeof(RangeComponent),
                typeof(EnemyTargetComponent),
            };
            
            spawnedEntity = entityManager.CreateEntity(componentTypes);
            entityManager.SetComponentData(spawnedEntity, new LocalTransform { Position = position });
            entityManager.SetComponentData(spawnedEntity, new RangeComponent { Range = Range });
        }

        public override Attack Attack => attack;

        public override LayerMask CollideWith => bombData.AttackLayerMask;

        public override void OnStateEntered()
        {

        }

        public override void OnSelected(Vector3 pos)
        {
            rangeIndicator = bombData.RangeIndicator.GetAtPosAndRot<PooledMonoBehaviour>(pos, Quaternion.identity).gameObject;
            rangeIndicator.transform.localScale = new Vector3(Range * 2.0f, 0.01f, Range * 2.0f);
        }

        public override void OnDeselected()
        {
            if (rangeIndicator != null)
            {
                rangeIndicator.SetActive(false);
            }
        }

        public override void Update()
        {
            if (attackCooldownTimer <= 0)
            {
                Entity targetEntity = entityManager.GetComponentData<EnemyTargetComponent>(spawnedEntity).Target;
                if (entityManager.Exists(targetEntity))
                {
                    PerformAttack(entityManager.GetComponentData<LocalTransform>(targetEntity).Position);
                }
                else
                {
                    attackCooldownTimer = 0.1f;
                }
            }
            else
            {
                attackCooldownTimer -= Time.deltaTime;
            }
        }

        private void PerformAttack(float3 targetPosition)
        {
            AttackPosition = targetPosition;
            attack.TriggerAttack(this);
            attackCooldownTimer = 1.0f / stats.AttackSpeed.Value;
        }

        public override void OnWaveStart(int houseCount)
        {
            MoneyManager.Instance.AddMoney(houseCount * bombData.IncomePerHouse);
        }

        public override void Die()
        {

        }
    }

    #endregion

}

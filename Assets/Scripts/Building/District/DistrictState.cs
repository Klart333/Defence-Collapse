﻿using Cysharp.Threading.Tasks;
using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Buildings.District
{
    [System.Serializable]
    public abstract class DistrictState : IAttacker
    {
        public event Action OnAttack;
        
        public float Range { get; set; }

        protected Stats stats;
        protected DistrictData districtData;
        protected DamageInstance lastDamageDone;
        protected Entity spawnedEntity;
        protected EntityManager entityManager;

        public virtual Attack Attack { get; }
        public DamageInstance LastDamageDone => lastDamageDone;
        public Stats Stats => stats;
        public Vector3 OriginPosition { get; protected set; }
        public Vector3 AttackPosition { get; set; }
        public abstract LayerMask LayerMask { get; }

        protected DistrictState(DistrictData districtData, Vector3 position)
        {
            this.districtData = districtData;
            OriginPosition = position;

            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            
            spawnedEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(spawnedEntity, new LocalTransform { Position = position });
        }

        public abstract void OnStateEntered();
        public abstract void Update();
        public abstract void OnSelected(Vector3 pos);
        public abstract void OnDeselected();
        public abstract void Die();
        public abstract void OnWaveStart(int houseCount);

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

        public ArcherState(DistrictData districtData, TowerData archerData, Vector3 position) : base(districtData, position)
        {
            this.archerData = archerData;
            Range = archerData.Range;

            attack = new Attack(archerData.BaseAttack);
            stats = new Stats(archerData.Stats);

            entityManager.AddComponentData(spawnedEntity, new RangeComponent { Range = Range });
            entityManager.AddComponentData(spawnedEntity, new EnemyTargetComponent());
        }

        public override Attack Attack => attack;

        public override LayerMask LayerMask => archerData.AttackLayerMask;

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
                Debug.Log("Attacking Entity: " + targetEntity);
                if (entityManager.Exists(targetEntity))
                {
                    PerformAttack(entityManager.GetComponentData<LocalTransform>(targetEntity).Position);
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

        public BombState(DistrictData districtData, TowerData bombData, Vector3 position) : base(districtData, position)
        {
            this.bombData = bombData;
            Range = bombData.Range;

            attack = new Attack(bombData.BaseAttack);
            stats = new Stats(bombData.Stats);
        }

        public override Attack Attack => attack;

        public override LayerMask LayerMask => bombData.AttackLayerMask;

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
                // Get Data from Entity
                //EnemyHealth closest = EnemyManager.Instance.GetClosestEnemy(OriginPosition);
                //if (closest == null)
                //    return;

                //if (Vector3.Distance(OriginPosition, closest.transform.position) <= Range)
                //{
                //    attackCooldownTimer = 1.0f / stats.AttackSpeed.Value;
                //    PerformAttack(closest).Forget(Debug.LogError);
                //}
            }
            else
            {
                attackCooldownTimer -= Time.deltaTime;
            }
        }

        private async UniTask PerformAttack(EnemyHealth target)
        {
            AttackPosition = target.transform.position;
            attack.TriggerAttack(this);

            float timer = attackCooldownTimer;
            while (timer > 0 && target.Health.Alive)
            {
                await UniTask.Yield();

                timer -= Time.deltaTime;
                AttackPosition = target.transform.position;
            }
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

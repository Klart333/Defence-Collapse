using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

[System.Serializable]
public abstract class BuildingState : IAttacker
{
    public event Action OnAttack;

    public BuildingState(BuildingData buildingData)
    {
        this.buildingData = buildingData;
    }

    public abstract void OnStateEntered();
    public abstract void Update(Building building);
    public abstract void OnSelected(Vector3 pos);
    public abstract void OnDeselected();
    public abstract void Die();
    public abstract void OnWaveStart(int houseCount);

    public virtual Attack Attack { get; }
    public float Range { get; set; }

    protected Stats stats;
    protected BuildingData buildingData;
    protected DamageInstance lastDamageDone;

    public Health Health => buildingData.Health;
    public DamageInstance LastDamageDone => lastDamageDone;
    public Stats Stats => stats;
    public Vector3 OriginPosition { get; protected set; }
    public Vector3 AttackPosition { get; set; }
    public abstract LayerMask LayerMask { get; }

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

public class ArcherState : BuildingState
{
    private Attack attack;
    private TowerData archerData;
    private GameObject rangeIndicator;

    private float attackCooldownTimer = 0;

    public ArcherState(BuildingData buildingData, TowerData archerData) : base(buildingData)
    {
        this.archerData = archerData;
        Range = archerData.Range;

        attack = new Attack(archerData.BaseAttack);
        stats = new Stats(archerData.Stats);
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

    public override void Update(Building building)
    {
        OriginPosition = building.transform.position + Vector3.up * 1.5f; // ya never know

        if (EnemyManager.Instance.Enemies.Count <= 0)
        {
            return;
        }

        if (attackCooldownTimer <= 0)
        {
            EnemyHealth closest = EnemyManager.Instance.GetClosestEnemy(building.transform.position);
            if (closest == null)
                return;

            if (Vector3.Distance(building.transform.position, closest.transform.position) <= Range)
            {
                attackCooldownTimer = 1.0f / stats.AttackSpeed.Value;
                PerformAttack(closest);
            }
        }
        else
        {
            attackCooldownTimer -= Time.deltaTime;
        }
    }

    private async void PerformAttack(EnemyHealth target)
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
        MoneyManager.Instance.AddMoney(houseCount * archerData.IncomePerHouse);
    }

    public override void Die()
    {

    }
}

#endregion

#region Bomb

public class BombState : BuildingState
{
    private Attack attack;
    private TowerData bombData;
    private GameObject rangeIndicator;

    private float attackCooldownTimer = 0;

    public BombState(BuildingData buildingData, TowerData bombData) : base(buildingData)
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

    public override void Update(Building building)
    {
        OriginPosition = building.transform.position + Vector3.up * 1.5f; // ya never know

        if (EnemyManager.Instance.Enemies.Count <= 0)
        {
            return;
        }

        if (attackCooldownTimer <= 0)
        {
            EnemyHealth closest = EnemyManager.Instance.GetClosestEnemy(building.transform.position);
            if (closest == null)
                return;

            if (Vector3.Distance(building.transform.position, closest.transform.position) <= Range)
            {
                attackCooldownTimer = 1.0f / stats.AttackSpeed.Value;
                PerformAttack(closest);
            }
        }
        else
        {
            attackCooldownTimer -= Time.deltaTime;
        }
    }

    private async void PerformAttack(EnemyHealth target)
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

#region Normal

public class NormalState : BuildingState
{
    private NormalHouseData data;

    public override LayerMask LayerMask => throw new NotImplementedException();

    public NormalState(BuildingData buildingData, NormalHouseData houseData) : base(buildingData)
    {
        data = houseData;

        stats = new Stats
        {
            MaxHealth = new Stat(data.MaxHealth)
        };
    }

    public override void OnStateEntered()
    {

    }

    public override void Update(Building building)
    {
        OriginPosition = building.transform.position + Vector3.up * 1.5f;
    }


    public override void Die()
    {

    }

    public override void OnSelected(Vector3 pos)
    {
        
    }

    public override void OnDeselected()
    {

    }

    public override void OnWaveStart(int houseCount)
    {
        MoneyManager.Instance.AddMoney(houseCount * data.IncomePerHouse);
    }
}

#endregion

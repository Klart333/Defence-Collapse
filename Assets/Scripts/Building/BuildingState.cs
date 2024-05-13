using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

[System.Serializable]
public abstract class BuildingState
{
    public abstract void OnStateEntered();
    public abstract void Update(Building building);
    public abstract void OnSelected(Vector3 pos);
    public abstract void OnDeselected();
    public abstract void Die();
    public abstract void OnWaveStart(int houseCount);

    public abstract Stats Stats { get; }
    public virtual Attack Attack { get; }
    public float Range { get; set; }
}

[System.Serializable]
public class ArcherState : BuildingState, IAttacker
{
    public event Action OnAttack;

    private Stats stats;
    private Attack attack;
    private ArcherData archerData;
    private BuildingData buildingData;
    private GameObject rangeIndicator;
    private DamageInstance lastDamageDone;

    private float attackCooldownTimer = 0;

    public Health Health => buildingData.Health;
    public DamageInstance LastDamageDone => lastDamageDone;
    public LayerMask LayerMask => archerData.AttackLayerMask;
    public Vector3 OriginPosition { get; private set; }
    public Vector3 AttackPosition { get; set; }
    public override Stats Stats => stats;
    public override Attack Attack => attack;

    public ArcherState(ArcherData archerData, BuildingData buildingData)
    {
        this.archerData = archerData;
        this.buildingData = buildingData;

        Range = archerData.Range;

        attack = new Attack(archerData.BaseAttack);
        stats = new Stats(archerData.Stats);
    }

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

        float timer = attackCooldownTimer / 2.0f;
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

    public void OnUnitDoneDamage(DamageInstance damageInstance)
    {
        lastDamageDone = damageInstance;

        Attack.OnDoneDamage(this);
    }

    public void OnUnitKill()
    {

    }

    public override void Die()
    {

    }

}

public class NormalState : BuildingState
{
    private NormalHouseData data;

    public override Stats Stats => null;

    public NormalState(NormalHouseData data)
    {
        this.data = data;
    }

    public override void OnStateEntered()
    {

    }

    public override void Update(Building building)
    {
        
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

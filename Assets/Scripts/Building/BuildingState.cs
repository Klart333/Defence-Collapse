using System.Threading.Tasks;
using UnityEngine;

public abstract class BuildingState
{
    public abstract void OnStateEntered();
    public abstract void Update(Building building);
    public abstract void OnSelected(Vector3 pos);
    public abstract void OnDeselected();
    public abstract void Die();
    public abstract void OnWaveStart(int houseCount);
}

public class ArcherState : BuildingState
{
    private ArcherData data;

    private GameObject rangeIndicator;

    private float attackCooldownTimer = 0;

    public ArcherState(ArcherData data)
    {
        this.data = data;
    }

    public override void OnStateEntered()
    {
        
    }

    public override void Update(Building building)
    {
        if (EnemyManager.Instance.Enemies.Count <= 0)
        {
            return;
        }

        if (attackCooldownTimer <= 0)
        {
            EnemyHealth closest = EnemyManager.Instance.GetClosestEnemy(building.transform.position);
            if (closest == null)
                return;

            if (Vector3.Distance(building.transform.position, closest.transform.position) <= data.Range)
            {
                Attack(closest, building);
                attackCooldownTimer = 1.0f / data.AttackSpeed;
            }
        }
        else
        {
            attackCooldownTimer -= Time.deltaTime;
        }
    }

    private async void Attack(EnemyHealth target, Building building)
    {
        Projectile arrow = data.Arrow.GetAtPosAndRot<Projectile>(building.transform.position, Quaternion.identity);
        arrow.Damage = data.Damage;

        float t = 0;

        Vector3 startPos = building.transform.position;
        Vector3 midPos = Vector3.Lerp(startPos, target.transform.position, 0.5f) + Vector3.up * 10f;

        while (t <= 1.0f)
        {
            t += Time.deltaTime;

            arrow.transform.position = Vector3.Lerp(Vector3.Lerp(startPos, midPos, t), Vector3.Lerp(midPos, target.transform.position, t), t);

            await Task.Yield();
        }
    }

    public override void Die()
    {

    }

    public override void OnSelected(Vector3 pos)
    {
        rangeIndicator = data.RangeIndicator.GetAtPosAndRot<PooledMonoBehaviour>(pos, Quaternion.identity).gameObject;
        rangeIndicator.transform.localScale = new Vector3(data.Range * 2.0f, 0.01f, data.Range * 2.0f);
    }

    public override void OnDeselected()
    {
        if (rangeIndicator != null)
        {
            rangeIndicator.SetActive(false);
        }
    }

    public override void OnWaveStart(int houseCount)
    {
        MoneyManager.Instance.AddMoney(houseCount * data.IncomePerHouse);
    }
}

public class NormalState : BuildingState
{
    private NormalHouseData data;

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

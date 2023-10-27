using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using static UnityEditor.Progress;

public abstract class BuildingState
{
    public abstract void WaveStarted();
    public abstract void OnStateEntered(Building building);
    public abstract void Update();
    public abstract void OnSelected();
    public abstract void OnDeSelected();
    public abstract void OnPlaced();
    public abstract void Die();
}

public class ArcherState : BuildingState
{
    private ArcherData data;
    private Building building;

    private GameObject rangeIndicator;

    private float attackCooldownTimer = 0;

    public ArcherState(ArcherData data)
    {
        this.data = data;
    }

    public override void OnPlaced()
    {
        
    }

    public override void OnStateEntered(Building building)
    {
        this.building = building;
    }

    public override void Update()
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
                Attack(closest);
                attackCooldownTimer = 1.0f / data.AttackSpeed;
            }
        }
        else
        {
            attackCooldownTimer -= Time.deltaTime;
        }
    }

    private async void Attack(EnemyHealth target)
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

    public override void WaveStarted()
    {

    }

    public override void Die()
    {

    }

    public override void OnSelected()
    {
        rangeIndicator = data.RangeIndicator.GetAtPosAndRot<PooledMonoBehaviour>(building.transform.position, Quaternion.identity).gameObject;
        rangeIndicator.transform.localScale = new Vector3(data.Range * 2.0f, 0.01f, data.Range * 2.0f);
    }

    public override void OnDeSelected()
    {
        if (rangeIndicator != null)
        {
            rangeIndicator.SetActive(false);
        }
    }
}

public class BarracksState : BuildingState
{
    private Building building;
    private BarrackData data;

    private bool inDefense;
    private Vector3 defensePosition;

    public BarracksState(BarrackData data)
    {
        this.data = data;
    }

    public override void OnStateEntered(Building building)
    {
        this.building = building;
    }

    private void SpawnFighter()
    {
        Fighter fighter = building.Fighters[UnityEngine.Random.Range(0, building.Fighters.Length)];

        Vector3 pos = building.transform.position + Vector3.up * 0.1f + Quaternion.AngleAxis(UnityEngine.Random.Range(0, 360), Vector3.up) * Vector3.forward;
        var spawnedFighter = GameObject.Instantiate(fighter, pos, Quaternion.identity);
        spawnedFighter.Building = building;

        building.SpawnedFighters.Add(spawnedFighter);

        spawnedFighter.GetComponentInChildren<IHealth>().OnDeath += BarracksState_OnDeath;
    }

    private void BarracksState_OnDeath(GameObject obj)
    {
        building.SpawnedFighters.Remove(obj.GetComponentInChildren<Fighter>());
    }

    public override void WaveStarted()
    {
        SpawnFighter();
    }

    public override void Die()
    {
        for (int i = 0; i < building.SpawnedFighters.Count; i++)
        {
            GameObject.Destroy(building.SpawnedFighters[i].gameObject);
        }

        building.SpawnedFighters.Clear();
    }

    public override void OnPlaced()
    {
        SpawnFighter();
    }

    public override void Update()
    {
        if (inDefense)
        {
            return;
        }

        if (building.AttackingEnemies.Count > 0)
        {
            EnemyMovement enemy = GetClosestEnemy(out float distance);

            defensePosition = enemy.transform.position;

            if (distance < data.AlarmRange)
            {
                Debug.Log("ALARM!");
                Debug.DrawRay(defensePosition, Vector3.up, Color.red, 10);
                inDefense = true;
                building.EnterDefense(defensePosition);
            }
        }
    }

    private EnemyMovement GetClosestEnemy(out float distance)
    {
        distance = 2048;
        EnemyMovement index = null;

        for (int i = 0; i < building.AttackingEnemies.Count; i++)
        {
            float dis = Vector3.Distance(building.AttackingEnemies[i].transform.position, building.transform.position);
            if (dis < distance)
            {
                distance = dis;
                index = building.AttackingEnemies[i];
            }
        }

        return index;
    }

    public override void OnSelected()
    {

    }

    public override void OnDeSelected()
    {

    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : Singleton<EnemyManager>
{
    private List<EnemyHealth> enemies = new List<EnemyHealth>();

    public List<EnemyHealth> Enemies => enemies;

    public void RegisterEnemy(EnemyHealth enemy)
    {
        enemies.Add(enemy);

        enemy.Health.OnDeath += Health_OnDeath;
    }

    private void Health_OnDeath(GameObject obj)
    {
        if (obj.TryGetComponent<EnemyHealth>(out EnemyHealth health))
        {
            enemies.Remove(health);
        }
    }

    public EnemyHealth GetClosestEnemy(Vector3 pos)
    {
        if (enemies.Count <= 0)
        {
            return null;
        }

        float distance = 2048;
        int index = 0;
        for (int i = 0; i < enemies.Count; i++)
        {
            float dist = Vector3.SqrMagnitude(enemies[i].transform.position - pos);
            if (dist < distance)
            {
                distance = dist;
                index = i;
            }
        }

        return enemies[index];
    }
}

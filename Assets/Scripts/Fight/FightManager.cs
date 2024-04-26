using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FightManager : Singleton<FightManager>
{
    private Dictionary<Vector3, FightData> fights = new Dictionary<Vector3, FightData>();
    private Dictionary<GameObject, Vector3> fighters = new Dictionary<GameObject, Vector3>();

    /*private void OnEnable()
    {
        GameEvents.OnFightStarted += OnFightStarted;
        GameEvents.OnFightStarted += OnFightEnded;
    }

    private void OnDisable()
    {
        GameEvents.OnFightStarted -= OnFightStarted;
        GameEvents.OnFightStarted -= OnFightEnded;
    }
*/
    private void OnFightStarted(Vector3 pos)
    {
        fights.Add(pos, new FightData());
    }

    private void OnFightEnded(Vector3 pos)
    {
        fights.Remove(pos);
    }

    public void JoinFight(Fighter fighter)
    {
        Vector3 pos = GetClosestFight(fighter.transform.position);

        if (fights.ContainsKey(pos))
        {
            fights[pos].Fighters.Add(fighter);

            fighters.Add(fighter.gameObject, pos);
            fighter.GetComponentInChildren<IHealth>().OnDeath += OnFighterDeath;
        }
    }

    public void JoinFight(EnemyAttacker enemy)
    {
        Vector3 pos = GetClosestFight(enemy.transform.position);

        if (fights.ContainsKey(pos))
        {
            fights[pos].Enemies.Add(enemy);

            fighters.Add(enemy.gameObject, pos);
            enemy.GetComponentInChildren<IHealth>().OnDeath += OnEnemyDeath;
        }
    }

    private Vector3 GetClosestFight(Vector3 position)
    {
        float distance = 1024;
        Vector3 pos = Vector3.zero;

        foreach (var item in fights)
        {
            float dist = Vector3.Distance(position, item.Key);
            if (dist < distance)
            {
                distance = dist;
                pos = item.Key;
            }
        }

        return pos;
    }

    private void OnFighterDeath(GameObject fighter)
    {
        fighter.GetComponentInChildren<IHealth>().OnDeath -= OnFighterDeath;

        fights[fighters[fighter]].Fighters.Remove(fighter.GetComponent<Fighter>());

        if (fights[fighters[fighter]].Fighters.Count <= 0)
        {
            //GameEvents.OnFightEnded(fighters[fighter]);
        }
        
        fighters.Remove(fighter);
    }

    private void OnEnemyDeath(GameObject enemy)
    {
        enemy.GetComponentInChildren<IHealth>().OnDeath -= OnFighterDeath;

        fights[fighters[enemy]].Enemies.Remove(enemy.GetComponent<EnemyAttacker>());

        if (fights[fighters[enemy]].Enemies.Count <= 0)
        {
            //GameEvents.OnFightEnded(fighters[enemy]);
        }

        fighters.Remove(enemy);
    }
}

public class FightData
{
    public List<Fighter> Fighters;
    public List<EnemyAttacker> Enemies;

    public FightData()
    {
        Fighters = new List<Fighter>();
        Enemies = new List<EnemyAttacker>();
    }

    public FightData(List<Fighter> fighters, List<EnemyAttacker> enemies)
    {
        Fighters = fighters;
        Enemies = enemies;
    }
}

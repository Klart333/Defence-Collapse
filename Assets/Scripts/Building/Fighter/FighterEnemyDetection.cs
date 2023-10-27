using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FighterEnemyDetection : MonoBehaviour
{
    private List<Vector3> fights = new List<Vector3>();

    private Fighter fighter;

    private void OnEnable()
    {
        fighter = GetComponentInParent<Fighter>();

        GameEvents.OnFightEnded += OnFightEnded;
        GameEvents.OnFightStarted += OnFightStarted;
    }

    private void OnDisable()
    {
        GameEvents.OnFightEnded -= OnFightEnded;
        GameEvents.OnFightStarted -= OnFightStarted;
    }

    private void OnFightStarted(Vector3 position)
    {
        fights.Add(position);
    }

    private void OnFightEnded(Vector3 position)
    {
        fights.Remove(position);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (fighter.Fighting)
        {
            return;
        }

        float distance = 0;
        for (int i = 0; i < fights.Count; i++)
        {
            float dist = Vector3.Distance(fights[i], transform.position);
            if (dist < distance)
            {
                distance = dist;
            }
        }

        if (distance < 5)
        {
            GameEvents.OnFightStarted(transform.position);
        }

        StartFighting();
    }

    private void StartFighting()
    {
        fighter.StartFighting();
    }
}

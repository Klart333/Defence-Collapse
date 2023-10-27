using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingHealth : MonoBehaviour, IHealth
{
    public event Action<GameObject> OnDeath;

    [SerializeField]
    private float maxHealth;

    private Building building;
    private Health health;

    private void OnEnable()
    {
        building = GetComponent<Building>();

        health = new Health(maxHealth, gameObject);
        health.OnDeath += Health_OnDeath;
    }

    private void OnDisable()
    {
        health.OnDeath -= Health_OnDeath;
    }

    private void Health_OnDeath(GameObject gameObject)
    {
        building.Die();
        OnDeath?.Invoke(gameObject);

        Destroy(gameObject);
    }

    public void TakeDamage(float amount)
    {
        health.TakeDamage(amount);
    }
}

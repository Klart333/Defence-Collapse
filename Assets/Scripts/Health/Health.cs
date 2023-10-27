using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Health : IHealth
{
    public event Action<GameObject> OnDeath;

    public float MaxHealth;
    public float CurrentHealth;

    private GameObject gameObject;

    public bool Alive => CurrentHealth > 0;

    public Health(float maxHealth, GameObject gameObject)
    {
        MaxHealth = maxHealth;
        CurrentHealth = MaxHealth;
        
        this.gameObject = gameObject;
    }

    public void TakeDamage(float amount)
    {
        if (!Alive)
        {
            return;
        }

        CurrentHealth -= amount;

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        OnDeath?.Invoke(gameObject);
    }
}

public interface IHealth
{
    public event Action<GameObject> OnDeath;

    public void TakeDamage(float amount);
}
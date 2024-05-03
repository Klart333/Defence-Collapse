using Sirenix.OdinInspector;
using System;
using UnityEngine;

[System.Serializable]
public class Health
{
    public event Action OnDeath;

    public float MaxHealth;
    public float CurrentHealth;

    public bool Alive => CurrentHealth > 0;

    public Health(float maxHealth)
    {
        MaxHealth = maxHealth;
        CurrentHealth = MaxHealth;
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
        OnDeath?.Invoke();
    }
}

public interface IHealth<T> // This "generic" interface is not generic at all >:(
{
    public event Action<T> OnDeath;

    public void TakeDamage(float amount);
}
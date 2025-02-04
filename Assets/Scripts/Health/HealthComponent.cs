using System.Collections.Generic;
using UnityEngine;
using System;

[System.Serializable]
public class HealthComponent : IHealth // THIS DOESN'T WORK WITH ECS SO WHATEVER!
{
    public event Action OnDeath;
    public event Action OnTakeDamage;

    public float CurrentHealth;
    public float MaxHealth;

    public bool Alive => CurrentHealth > 0;
    public float HealthPercentage => CurrentHealth / Stats.MaxHealth.Value;

    private readonly Transform transform;
    
    public List<StatusEffect> StatusEffects { get; set; } = new List<StatusEffect>();
    public DamageInstance LastDamageTaken { get; private set; }
    public Stats Stats { get; private set; }
    
    public Vector3 OriginPosition => transform.position;
    public HealthComponent Health => this;
    
    public HealthComponent(Stats stats, Transform transform)
    {
        Stats = stats;
        this.transform = transform;
        
        Stats.MaxHealth.OnValueChanged += UpdateMaxHealth;
        UpdateMaxHealth();
    }

    private void UpdateMaxHealth()
    {
        SetMaxHealth(Stats.MaxHealth.Value);
    }

    public void SetMaxHealth(float maxHealth)
    {
        float diff = maxHealth - MaxHealth;

        MaxHealth = maxHealth;
        CurrentHealth += diff;
    }

    public void TakeDamage(DamageInstance damage, out DamageInstance damageDone)
    {
        if (!Alive || damage.Damage <= 0.1f)
        {
            damageDone = damage;
            return;
        }

        damageDone = new DamageInstance(damage);
        for (int i = 0; i < StatusEffects.Count; i++)
        {
            StatusEffects[i].TriggerEFfect(ref damageDone);
        }

        LastDamageTaken = damageDone;
        damageDone.TargetHit = this;

        CurrentHealth -= damageDone.Damage;

        OnTakeDamage?.Invoke();

        if (CurrentHealth <= 0)
        {
            Die(damageDone);
        }
    }
    
    private void Die(DamageInstance killingDamage)
    {
        killingDamage.Source.OnUnitKill();

        OnDeath?.Invoke();
    }

}

public interface IHealth
{
    public HealthComponent Health { get; }
    public Vector3 OriginPosition { get; }
    
    public void TakeDamage(DamageInstance damage, out DamageInstance damageDone);
}
using System.Collections.Generic;
using System;

[System.Serializable]
public class HealthComponent : IHealth
{
    public event Action OnDeath;
    public event Action OnHealthChanged;

    public float CurrentHealth;
    public float MaxHealth;
    
    public bool Alive => CurrentHealth > 0;
    public float HealthPercentage => CurrentHealth / Stats.MaxHealth.Value;
    
    public List<StatusEffect> StatusEffects { get; set; } = new List<StatusEffect>();
    public DamageInstance LastDamageTaken { get; private set; }
    public Stats Stats { get; private set; }
    
    public HealthComponent Health => this;
    
    public HealthComponent(Stats stats)
    {
        Stats = stats;
        
        Stats.MaxHealth.OnValueChanged += UpdateMaxHealth;
        UpdateMaxHealth();
    }

    private void UpdateMaxHealth()
    {
        SetMaxHealth(Stats.MaxHealth.Value);
    }

    private void SetMaxHealth(float maxHealth)
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
        OnHealthChanged?.Invoke();

        if (CurrentHealth <= 0)
        {
            Die(damageDone);
        }
    }
    
    public void TakeDamage(float damage)
    {
        if (!Alive)
        {
            return;
        }

        var damageDone = new DamageInstance(damage);
        for (int i = 0; i < StatusEffects.Count; i++)
        {
            StatusEffects[i].TriggerEFfect(ref damageDone);
        }

        CurrentHealth -= damageDone.Damage;
        OnHealthChanged?.Invoke();

        if (CurrentHealth <= 0)
        {
            Die(damageDone);
        }
    }
    
    private void Die(DamageInstance killingDamage)
    {
        killingDamage.Source?.OnUnitKill();

        OnDeath?.Invoke();
    }

    public void SetHealthToMax()
    {
        CurrentHealth = MaxHealth;
        OnHealthChanged?.Invoke();
    }
}

public interface IHealth
{
    public HealthComponent Health { get; }
    
    public void TakeDamage(DamageInstance damage, out DamageInstance damageDone);
}
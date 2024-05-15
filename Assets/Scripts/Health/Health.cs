using System;
using System.Collections.Generic;

[System.Serializable]
public class Health : IHealth
{
    public event Action OnDeath;
    public event Action OnTakeDamage;

    public float MaxHealth;
    public float CurrentHealth;

    public bool Alive => CurrentHealth > 0;
    public float HealthPercentage => CurrentHealth / MaxHealth;

    public List<StatusEffect> StatusEffects { get; set; } = new List<StatusEffect>();
    public DamageInstance LastDamageTaken { get; private set; }
    public IAttacker Attacker { get; private set; }

    public Health(IAttacker attacker)
    {
        UpdateAttacker(attacker);
    }

    public void UpdateAttacker(IAttacker attacker)
    {
        Attacker = attacker;
        attacker.Stats.MaxHealth.OnValueChanged += UpdateMaxHealth;

        UpdateMaxHealth();
    }

    private void UpdateMaxHealth()
    {
        SetMaxHealth(Attacker.Stats.MaxHealth.Value);
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

        for (int i = 0; i < StatusEffects.Count; i++)
        {
            StatusEffects[i].TriggerEFfect(ref damage);
        }

        LastDamageTaken = damage;
        damageDone = damage;
        damageDone.TargetHit = this;

        CurrentHealth -= damage.Damage;

        OnTakeDamage?.Invoke();

        if (CurrentHealth <= 0)
        {
            Die(damage);
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
    public void TakeDamage(DamageInstance damage, out DamageInstance damageDone);

    public IAttacker Attacker { get; }
}
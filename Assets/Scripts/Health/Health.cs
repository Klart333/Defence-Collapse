using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Health : IHealth
{
    public event Action OnDeath;

    public float MaxHealth;
    public float CurrentHealth;

    public DamageInstance LastDamageTaken { get; private set; }
    public bool Alive => CurrentHealth > 0;
    public float HealthPercentage => CurrentHealth / MaxHealth;
    public List<StatusEffect> StatusEffects { get; set; } = new List<StatusEffect>();

    public Vector3 Position => throw new NotImplementedException();

    public Health(float maxHealth)
    {
        MaxHealth = maxHealth;
        CurrentHealth = MaxHealth;
    }

    public void TakeDamage(DamageInstance damage, out DamageInstance damageDone)
    {
        if (!Alive)
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
        //damageDone.TargetHit = this; 

        CurrentHealth -= damage.Damage;

        //SpawnDamageNumber(damage);
        //Shake();
        //Fresnel();

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

    public void SetMaxHealth(float maxHealth)
    {
        float diff = maxHealth - MaxHealth;

        MaxHealth = maxHealth;
        CurrentHealth += diff;
    }

    /*
        private void Fresnel()
        {
            meshRenderer.GetPropertyBlock(block);

            Color color = new Color(1, 0, 0, 0);
            Color targetColor = new Color(1, 0, 0, 1);
            DOTween.To(() => color,
                       (x) =>
                       {
                           color = x;
                           block.SetColor("_Fresnel", color);
                           meshRenderer.SetPropertyBlock(block);
                       },
                       targetColor,
                       0.4f).SetLoops(2, LoopType.Yoyo).SetEase(Ease.OutCirc);

        }

        private void Shake()
        {
            transform.DORewind();

            //transform.DOShakePosition(0.05f, 0.1f);
            transform.DOPunchScale(Vector3.one * 0.05f, 0.1f);
        }

        private void SpawnDamageNumber(DamageInstance dmg)
        {
            Vector3 position = transform.position + new Vector3(UnityEngine.Random.Range(-0.4f, 0.4f), 0.5f, UnityEngine.Random.Range(-0.4f, 0.4f));
            DamageNumber num = damageNumber.GetAtPosAndRot<DamageNumber>(position, Quaternion.identity);
            num.SetDamage(dmg);
        }
    */

}

public interface IHealth
{
    public void TakeDamage(DamageInstance damage, out DamageInstance damageDone);

    public Vector3 Position { get; }
}
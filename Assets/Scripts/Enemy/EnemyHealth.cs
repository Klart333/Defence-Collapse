using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour, IHealth<EnemyHealth>
{
    public event Action<EnemyHealth> OnDeath;

    [SerializeField]
    private float maxHealth;

    [SerializeField]
    private PooledMonoBehaviour hitParticle;

    private EnemyAnimator animator;
    private Health health;

    public Health Health => health;

    private void OnEnable()
    {
        animator = GetComponent<EnemyAnimator>();

        health = new Health(maxHealth);
        health.OnDeath += Health_OnDeath;

        EnemyManager.Instance.RegisterEnemy(this);
    }

    private void OnDisable()
    {
        health.OnDeath -= Health_OnDeath;
    }

    private void Health_OnDeath()
    {
        OnDeath?.Invoke(this);

        animator.Die(ActuallyDie);
    }

    private void ActuallyDie()
    {
        Destroy(gameObject);
        //gameObject.SetActive(false);
    }

    public void TakeDamage(float amount)
    {
        health.TakeDamage(amount);

        hitParticle.GetAtPosAndRot<PooledMonoBehaviour>(transform.position, hitParticle.transform.rotation);
    }

}

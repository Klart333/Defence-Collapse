using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour, IHealth
{
    public event Action<GameObject> OnDeath;

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

        health = new Health(maxHealth, gameObject);
        health.OnDeath += Health_OnDeath;

        EnemyManager.Instance.RegisterEnemy(this);
    }

    private void OnDisable()
    {
        health.OnDeath -= Health_OnDeath;
    }

    private void Health_OnDeath(GameObject obj)
    {
        OnDeath?.Invoke(obj);

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

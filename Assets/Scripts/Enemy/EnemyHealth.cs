using Sirenix.OdinInspector;
using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour, IHealth
{
    public event Action<EnemyHealth> OnDeath;

    [Title("Data")]
    [SerializeField]
    private EnemyAttackData enemyData;

    [Title("Juice")]
    [SerializeField]
    private DamageNumber damageNumber;

    [SerializeField]
    private PooledMonoBehaviour hitParticle;

    private EnemyAnimator animator;
    private Health health;

    public Health Health => health;

    private void OnEnable()
    {
        animator = GetComponent<EnemyAnimator>();

        health = new Health(enemyData.MaxHealth);
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

    public void TakeDamage(DamageInstance damage, out DamageInstance damageDone)
    {
        health.TakeDamage(damage, out damageDone);

        hitParticle.GetAtPosAndRot<PooledMonoBehaviour>(transform.position, hitParticle.transform.rotation);
    }

}

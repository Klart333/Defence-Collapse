using Sirenix.OdinInspector;
using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour, IHealth
{
    public event Action<EnemyHealth> OnDeath;

    [Title("Data")]
    [SerializeField]
    private EnemyData enemyData;

    [Title("Juice")]
    [SerializeField]
    private DamageNumber damageNumber;

    [SerializeField]
    private PooledMonoBehaviour hitParticle;

    private EnemyAnimator animator;
    private Health health;

    public Health Health => health;
    public Vector3 Position => transform.position;

    private void OnEnable()
    {
        animator = GetComponent<EnemyAnimator>();

        health = new Health(enemyData.Stats.MaxHealth.Value);
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

        int grade = LootManager.Instance.GetGrade(enemyData.LootProbability);
        if (grade >= 0)
        {
            LootManager.Instance.SpawnLoot(transform.position + Vector3.up * 0.1f, 1, grade);
        }
    }

    public void TakeDamage(DamageInstance damage, out DamageInstance damageDone)
    {
        health.TakeDamage(damage, out damageDone);

        hitParticle.GetAtPosAndRot<PooledMonoBehaviour>(transform.position, hitParticle.transform.rotation);
    }

}

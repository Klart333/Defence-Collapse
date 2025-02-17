using Sirenix.OdinInspector;
using UnityEngine;
using System;

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

    [Title("Healthbar")]
    [SerializeField]
    private EnemyHealthbar healthbar;

    private EnemyAttacker attacker;
    private EnemyAnimator animator;
    private HealthComponent health;

    public HealthComponent Health => health;
    public Vector3 OriginPosition => Health.OriginPosition;
    public IAttacker Attacker => attacker;

    private void OnEnable()
    {
        animator = GetComponent<EnemyAnimator>();
        attacker = GetComponent<EnemyAttacker>();

        health = new HealthComponent(Attacker.Stats, transform);
        health.OnDeath += HealthOnDeath;

        healthbar.Setup(this);
    }

    private void OnDisable()
    {
        healthbar.Reset();

        health.OnDeath -= HealthOnDeath;
    }

    private void HealthOnDeath()
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
        if (!health.Alive || damage.Damage <= 0.1f)
        {
            damageDone = damage;
            return;
        }

        health.TakeDamage(damage, out damageDone);

        hitParticle.GetAtPosAndRot<PooledMonoBehaviour>(transform.position, hitParticle.transform.rotation);

        SpawnDamageNumber(damage);
    }

    private void SpawnDamageNumber(DamageInstance dmg)
    {
        Vector3 position = transform.position + new Vector3(UnityEngine.Random.Range(-0.4f, 0.4f), 0.5f, UnityEngine.Random.Range(-0.4f, 0.4f));
        DamageNumber num = damageNumber.GetAtPosAndRot<DamageNumber>(position, Quaternion.identity);
        num.SetDamage(dmg);
    }

}

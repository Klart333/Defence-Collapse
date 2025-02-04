using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAttacker : MonoBehaviour, IAttacker, IHealth
{
    public event Action OnAttack;

    [SerializeField]
    private EnemyData attackData;

    private Stats stats;
    private EnemyAnimator animator;
    private EnemyHealth health;

    private float attackTimer = 0;

    public bool Attacking { get; private set; }

    public Stats Stats => stats; 
    public HealthComponent Health => health.Health;
    public DamageInstance LastDamageDone { get; private set; }
    public Vector3 AttackPosition { get; set; }
    public Vector3 OriginPosition => transform.position;
    public LayerMask LayerMask => attackData.LayerMask;
    public IAttacker Attacker => this;

    private void Awake()
    {
        animator = GetComponent<EnemyAnimator>();
        health = GetComponent<EnemyHealth>();

        stats = new Stats(attackData.Stats);
    }

    private void Update()
    {
        if (!health.Health.Alive)
        {
            return;
        }

        if (Attacking)
        {
            attackTimer += Time.deltaTime;

            if (attackTimer >= 1.0f / Stats.AttackSpeed.Value)
            {
                attackTimer = 0;
                Attack();
            }
        }   
    }

    private void Attack()
    {
        animator.Attack();

        AttackPosition = transform.position + transform.forward * attackData.AttackRadius;
        attackData.BaseAttack.TriggerAttack(this);
    }

    public void StartAttacking() // Want to replace with responding to events
    {
        Attacking = true;
    }

    public void StopAttacking()
    {
        Attacking = false;
    }

    public void OnUnitDoneDamage(DamageInstance damageInstance)
    {
        LastDamageDone = damageInstance;
    }

    public void OnUnitKill()
    {

    }

    public void TakeDamage(DamageInstance damage, out DamageInstance damageDone)
    {
        health.TakeDamage(damage, out damageDone);
    }
}

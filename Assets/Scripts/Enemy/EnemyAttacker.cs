using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAttacker : MonoBehaviour
{
    [SerializeField]
    private EnemyAttackData attackData;

    private Collider[] hitResults;

    private EnemyAnimator animator;
    private EnemyHealth health;

    private float attackTimer = 0;

    public bool Attacking { get; private set; }

    private void Start()
    {
        animator = GetComponent<EnemyAnimator>();
        health = GetComponent<EnemyHealth>();

        hitResults = new Collider[10];
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

            if (attackTimer >= 1.0f / attackData.AttackSpeed)
            {
                attackTimer = 0;
                Attack();
            }
        }   
    }

    private void Attack()
    {
        animator.Attack();

        Vector3 pos = transform.position + transform.forward * attackData.AttackRadius;
        Physics.OverlapSphereNonAlloc(pos, attackData.AttackRadius, hitResults, attackData.LayerMask);

        int damageDones = 0;
        for (int i = 0; i < hitResults.Length; i++)
        {
            if (hitResults[i] == null)
            {
                continue;
            }

            if (!hitResults[i].gameObject.activeSelf)
            {
                continue;
            }

            if (hitResults[i].TryGetComponent<IHealth>(out IHealth health))
            {
                health.TakeDamage(attackData.Damage);

                if (!attackData.Splash || ++damageDones >= attackData.MaxTargets)
                {
                    break;
                }
            }
        }
    }

    public void StartAttacking()
    {
        FightManager.Instance.JoinFight(this);
        Attacking = true;
    }

    internal void StopAttacking()
    {
        Attacking = false;
    }
}

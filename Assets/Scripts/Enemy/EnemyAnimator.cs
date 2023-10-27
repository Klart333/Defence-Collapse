using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class EnemyAnimator : MonoBehaviour
{
    [SerializeField]
    private float deathLength = 1f;

    private Animator cachedAnimator;
    private Animator animator
    {
        get
        {
            if (cachedAnimator == null)
            {
                cachedAnimator = GetComponentInChildren<Animator>();
            }

            return cachedAnimator;
        }
    }

    public void Attack()
    {
        animator.SetTrigger("Attack");
    }

    public async void Die(Action callback)
    {
        animator.SetTrigger("Die");

        await Task.Delay((int)(deathLength * 1000));
        callback();
    }

    public void Move()
    {
        animator.SetBool("Moving", true);
    }

    public void StopMoving()
    {
        animator.SetBool("Moving", false);
    }
}

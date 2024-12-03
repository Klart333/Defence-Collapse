using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using System;

public class EnemyAnimator : MonoBehaviour
{
    [SerializeField]
    private float deathLength = 1f;

    [SerializeField, ReadOnly]
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

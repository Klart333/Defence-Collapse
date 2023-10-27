using System;
using UnityEngine;

public class FighterAnimator : MonoBehaviour
{
    private Animator animator;

    private void Start()
    {
        animator = GetComponent<Animator>();
    }

    public void Attack()
    {
        animator.SetTrigger("Attack");
    }

    public void Die()
    {
        animator.SetInteger("Health", 0);
    }

    public void Hit()
    {
        animator.SetTrigger("Hit");
    }
}
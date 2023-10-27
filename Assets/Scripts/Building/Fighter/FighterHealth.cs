using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FighterHealth : MonoBehaviour, IHealth
{
    public event Action<GameObject> OnDeath;

    [SerializeField]
    private int maxHealth;

    private Health health;
    private FighterAnimator animator;

    public Health Health => health;

    private void Start()
    {
        animator = GetComponentInChildren<FighterAnimator>();
        if (animator == null)
        {
            animator = GetComponentInParent<FighterAnimator>();
        }

        health = new Health(maxHealth, gameObject);
        health.OnDeath += Health_OnDeath;
    }

    private void Health_OnDeath(GameObject obj)
    {
        animator.Die();

        OnDeath?.Invoke(obj);
    }

    public void TakeDamage(float amount)
    {
        if (!health.Alive)
        {
            return;
        }

        health.TakeDamage(amount);

        animator.Hit();
    }
}

using Sirenix.OdinInspector;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    [Title("Stats")]
    [SerializeField]
    private EnemyData enemyData;

    [Title("Less important stats")]
    [SerializeField]
    private float turnSpeed = 0.05f;

    [SerializeField]
    private LayerMask layerMask;

    private EnemyAttacker attacker;
    private EnemyAnimator animator;
    private EnemyHealth health;

    private bool shouldMove = true;

    private void OnEnable()
    {
        attacker = GetComponent<EnemyAttacker>();
        health = GetComponent<EnemyHealth>();
        animator = GetComponent<EnemyAnimator>();

        //agent.speed = enemyData.Stats.MovementSpeed.Value;

        health.OnDeath += Health_OnDeath;
    }

    private void OnDisable()
    {
        health.OnDeath -= Health_OnDeath;
    }

    private void Update()
    {
        if (!health.Health.Alive) return;

        if (shouldMove)
        {
            Move();
        }
    }

    private void Move()
    {

    }

    private void StartAttacking()
    {
        animator.StopMoving();
        attacker.StartAttacking();
    }

    private void StopAttacking()
    {
        attacker.StopAttacking();
        animator.Move();
    }

    private void Health_OnDeath(EnemyHealth obj)
    {
        StopMoving();
    }

    private void StopMoving()
    {
        shouldMove = false;
    }
}
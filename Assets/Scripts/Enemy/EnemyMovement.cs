using Sirenix.OdinInspector;
using System;
using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    [Title("Stats")]
    [SerializeField]
    private float moveSpeed = 1;

    [Title("Less important stats")]
    [SerializeField]
    private float turnSpeed = 0.05f;

    [SerializeField]
    private LayerMask layerMask;

    private NavMeshAgent agent;
    private EnemyAttacker attacker;
    private EnemyAnimator animator;
    private EnemyHealth health;

    private Vector3 currentTarget;

    private void OnEnable()
    {
        attacker = GetComponent<EnemyAttacker>();
        health = GetComponent<EnemyHealth>();
        animator = GetComponent<EnemyAnimator>();
        agent = GetComponent<NavMeshAgent>();

        agent.speed = moveSpeed;

        Events.OnEnemyPathUpdated += OnEnemyPathUpdated;
        health.OnDeath += Health_OnDeath;
    }

    private void OnDisable()
    {
        Events.OnEnemyPathUpdated -= OnEnemyPathUpdated;
        health.OnDeath -= Health_OnDeath;
    }

    private void Health_OnDeath(EnemyHealth obj)
    {
        StopMoving();
        agent.enabled = false;
    }

    private void Update()
    {
        if (!health.Health.Alive) return;

        if (agent.isOnNavMesh && agent.remainingDistance < agent.stoppingDistance)
        {
            StartAttacking();
        }
    }

    private void OnEnemyPathUpdated(Vector3 oldTargetPos, Vector3 newTargetPos)
    {
        if (!health.Health.Alive || Vector3.Distance(oldTargetPos, currentTarget) > 1.0f)
        {
            return;
        }

        StopAttacking();
        SetPathTarget(newTargetPos);
    }

    public void SetPathTarget(Vector3 target)
    {
        if (attacker.Attacking || !health.Health.Alive)
        {
            return;
        }

        if (!NavMesh.SamplePosition(target, out NavMeshHit hit, 1f, NavMesh.AllAreas))
        {
            Debug.LogError("Could not set path to target position: " + target);
        }
        currentTarget = hit.position;

        UpdateNavAgent();
    }

    private void UpdateNavAgent()
    {
        if (!health.Health.Alive) return;

        animator.Move();
        if (!agent.isOnNavMesh)
        {
            Debug.LogError("Enemy not on navmesh");
            return;
        }

        if (!agent.SetDestination(currentTarget))
        {
            Debug.LogError("Could not calculate path to target position");
        }
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

    private void StopMoving()
    {
        agent.isStopped = true;
    }

}